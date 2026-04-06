using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Briefing;
using PreflightApi.Infrastructure.Dtos.Mappers;
using PreflightApi.Infrastructure.Dtos.Notam;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.Services;

public class BriefingService : IBriefingService
{
    private readonly PreflightApiDbContext _context;
    private readonly IAirportService _airportService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BriefingService> _logger;
    private readonly GeometryFactory _geometryFactory;
    private readonly int NAUTICAL_MILES_TO_METERS = 1852;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public BriefingService(
        PreflightApiDbContext context,
        IAirportService airportService,
        IServiceScopeFactory scopeFactory,
        ILogger<BriefingService> logger)
    {
        _context = context;
        _airportService = airportService;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
    }

    public async Task<RouteBriefingResponse> GetRouteBriefingAsync(RouteBriefingRequest request, CancellationToken ct = default)
    {
        ValidateRequest(request);

        // Phase 1: Batch-resolve all waypoints (single query for all airport waypoints)
        var resolvedCoords = await ResolveWaypointsAsync(request.Waypoints, ct);

        var routeLine = BuildRouteLineString(resolvedCoords);
        var corridorMeters = request.CorridorWidthNm * NAUTICAL_MILES_TO_METERS;
        var routeDescription = BuildRouteDescription(request.Waypoints);

        _logger.LogInformation(
            "Generating route briefing for {Route} with {CorridorNm} NM corridor",
            routeDescription, request.CorridorWidthNm);

        var waypointAirportIdents = request.Waypoints
            .Where(IsAirportWaypoint)
            .Select(w => w.AirportIdentifier!.ToUpperInvariant().Trim())
            .Distinct()
            .ToList();

        // Phase 2: All spatial queries in parallel (each gets its own DbContext via scope)
        var airportsTask = RunInScopeAsync(ctx => FindAirportsAlongRouteAsync(ctx, routeLine, corridorMeters, ct));
        var pirepsTask = RunInScopeAsync(ctx => FindPirepsAlongRouteAsync(ctx, routeLine, corridorMeters, ct));
        var sigmetsTask = RunInScopeAsync(ctx => FindSigmetsIntersectingRouteAsync(ctx, routeLine, ct));
        var gairmetsTask = RunInScopeAsync(ctx => FindGAirmetsIntersectingRouteAsync(ctx, routeLine, ct));
        var notamsTask = RunInScopeAsync(ctx => FindNotamsForBriefingAsync(ctx, routeLine, corridorMeters, waypointAirportIdents, ct));

        await Task.WhenAll(airportsTask, pirepsTask, sigmetsTask, gairmetsTask, notamsTask);

        var airports = await airportsTask;
        var pireps = await pirepsTask;
        var sigmets = await sigmetsTask;
        var gairmets = await gairmetsTask;
        var notams = await notamsTask;

        // Phase 3: METAR + TAF in parallel (each in its own scope)
        var airportIdents = airports
            .Select(a => a.IcaoId ?? a.ArptId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Cast<string>()
            .Distinct()
            .ToArray();

        var metars = new List<MetarDto>();
        var tafs = new List<TafDto>();

        if (airportIdents.Length > 0)
        {
            var metarTask = RunServiceInScopeAsync<IMetarService, IEnumerable<MetarDto>>(
                svc => svc.GetMetarsForAirports(airportIdents, ct));
            var tafTask = RunServiceInScopeAsync<ITafService, IEnumerable<TafDto>>(
                svc => svc.GetTafsForAirports(airportIdents, ct));

            await Task.WhenAll(metarTask, tafTask);
            metars = (await metarTask).ToList();
            tafs = (await tafTask).ToList();
        }

        return new RouteBriefingResponse
        {
            Route = routeDescription,
            CorridorWidthNm = request.CorridorWidthNm,
            GeneratedAt = DateTime.UtcNow,
            Metars = metars,
            Tafs = tafs,
            Pireps = pireps,
            Sigmets = sigmets,
            GAirmets = gairmets,
            Notams = notams,
            Summary = new RouteBriefingSummary
            {
                MetarCount = metars.Count,
                TafCount = tafs.Count,
                PirepCount = pireps.Count,
                SigmetCount = sigmets.Count,
                GAirmetCount = gairmets.Count,
                NotamCount = notams.Count
            }
        };
    }

    /// <summary>
    /// Creates a new DI scope with its own DbContext for parallel database work.
    /// DbContext is not thread-safe, so each concurrent query needs its own instance.
    /// </summary>
    private async Task<T> RunInScopeAsync<T>(Func<PreflightApiDbContext, Task<T>> work)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PreflightApiDbContext>();
        return await work(context);
    }

    /// <summary>
    /// Creates a new DI scope to resolve a service for parallel execution.
    /// </summary>
    private async Task<T> RunServiceInScopeAsync<TService, T>(Func<TService, Task<T>> work)
        where TService : notnull
    {
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TService>();
        return await work(service);
    }

    private void ValidateRequest(RouteBriefingRequest request)
    {
        if (request.Waypoints.Count < 2)
            throw new ValidationException("waypoints", "At least two waypoints are required to define a route");

        if (request.CorridorWidthNm <= 0)
            throw new ValidationException("corridorWidthNm", "Corridor width must be greater than 0");

        if (request.CorridorWidthNm > 100)
            throw new ValidationException("corridorWidthNm", "Corridor width cannot exceed 100 nautical miles");

        for (var i = 0; i < request.Waypoints.Count; i++)
        {
            var wp = request.Waypoints[i];
            if (IsAirportWaypoint(wp)) continue;

            if (!wp.Latitude.HasValue || !wp.Longitude.HasValue)
                throw new ValidationException("waypoints", $"Waypoint {i + 1}: latitude and longitude are required for coordinate waypoints");

            if (wp.Latitude.Value < -90 || wp.Latitude.Value > 90)
                throw new ValidationException("waypoints", $"Waypoint {i + 1}: latitude must be between -90 and 90");

            if (wp.Longitude.Value < -180 || wp.Longitude.Value > 180)
                throw new ValidationException("waypoints", $"Waypoint {i + 1}: longitude must be between -180 and 180");
        }
    }

    private async Task<List<(double lon, double lat, string label)>> ResolveWaypointsAsync(
        List<BriefingWaypoint> waypoints, CancellationToken ct)
    {
        // Batch-resolve all airport waypoints in a single query instead of N individual lookups
        var airportIdentifiers = waypoints
            .Where(IsAirportWaypoint)
            .Select(wp => wp.AirportIdentifier!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var airportLookup = new Dictionary<string, AirportDto>(StringComparer.OrdinalIgnoreCase);
        if (airportIdentifiers.Length > 0)
        {
            var airports = await _airportService.GetAirportsByIcaoCodesOrIdents(airportIdentifiers, ct);
            foreach (var airport in airports)
            {
                if (airport.IcaoId != null) airportLookup.TryAdd(airport.IcaoId, airport);
                if (airport.ArptId != null) airportLookup.TryAdd(airport.ArptId, airport);
            }
        }

        var resolved = new List<(double lon, double lat, string label)>();
        foreach (var wp in waypoints)
        {
            if (IsAirportWaypoint(wp))
            {
                var ident = wp.AirportIdentifier!;
                var candidates = AirportIdentifierResolver.GetCandidateIdentifiers(ident);
                var airport = candidates
                    .Select(c => airportLookup.GetValueOrDefault(c))
                    .FirstOrDefault(a => a != null);

                if (airport == null)
                    throw new AirportNotFoundException(ident);

                if (airport.LatDecimal == null || airport.LongDecimal == null)
                    throw new ValidationException("waypoints",
                        $"Airport '{ident}' does not have coordinates on record");

                resolved.Add(((double)airport.LongDecimal.Value, (double)airport.LatDecimal.Value,
                    airport.IcaoId ?? airport.ArptId ?? ident));
            }
            else
            {
                resolved.Add(((double)wp.Longitude!.Value, (double)wp.Latitude!.Value,
                    $"{wp.Latitude:F4},{wp.Longitude:F4}"));
            }
        }

        return resolved;
    }

    private LineString BuildRouteLineString(List<(double lon, double lat, string label)> coords)
    {
        var coordinates = coords.Select(c => new Coordinate(c.lon, c.lat)).ToArray();
        return _geometryFactory.CreateLineString(coordinates);
    }

    private static string BuildRouteDescription(List<BriefingWaypoint> waypoints)
    {
        return string.Join(" -> ", waypoints.Select(wp =>
            IsAirportWaypoint(wp)
                ? wp.AirportIdentifier!.ToUpperInvariant()
                : $"{wp.Latitude:F4},{wp.Longitude:F4}"));
    }

    private async Task<List<AirportDto>> FindAirportsAlongRouteAsync(
        PreflightApiDbContext context, LineString routeLine, double corridorMeters, CancellationToken ct)
    {
        var airports = await context.Airports
            .FromSqlInterpolated(
                $"SELECT * FROM airports WHERE location IS NOT NULL AND ST_DWithin(location::geography, {routeLine}::geography, {corridorMeters})")
            .AsNoTracking()
            .ToListAsync(ct);

        return airports.Select(a => AirportMapper.ToDto(a, _logger)).ToList();
    }

    private async Task<List<PirepDto>> FindPirepsAlongRouteAsync(
        PreflightApiDbContext context, LineString routeLine, double corridorMeters, CancellationToken ct)
    {
        var pireps = await context.Pireps
            .FromSqlInterpolated(
                $"SELECT * FROM pirep WHERE location IS NOT NULL AND ST_DWithin(location::geography, {routeLine}::geography, {corridorMeters})")
            .AsNoTracking()
            .ToListAsync(ct);

        return pireps.Select(p => PirepMapper.ToDto(p, _logger)).ToList();
    }

    private async Task<List<SigmetDto>> FindSigmetsIntersectingRouteAsync(
        PreflightApiDbContext context, LineString routeLine, CancellationToken ct)
    {
        var sigmets = await context.Sigmets
            .AsNoTracking()
            .Where(s => s.Boundary != null && s.Boundary.Intersects(routeLine))
            .ToListAsync(ct);

        return sigmets.Select(SigmetMapper.ToDto).ToList();
    }

    private async Task<List<GAirmetDto>> FindGAirmetsIntersectingRouteAsync(
        PreflightApiDbContext context, LineString routeLine, CancellationToken ct)
    {
        var gairmets = await context.GAirmets
            .AsNoTracking()
            .Where(g => g.Boundary != null && g.Boundary.Intersects(routeLine))
            .ToListAsync(ct);

        return gairmets.Select(g => GAirmetMapper.ToDto(g, _logger)).ToList();
    }

    /// <summary>
    /// Fetches NOTAMs in two parts mirroring a real preflight briefing:
    /// 1. Airport NOTAMs — by location identifier for the waypoint airports only
    /// 2. En-route NOTAMs — all NOTAMs with geometry within the spatial corridor (TFRs, airspace, etc.)
    /// Deduplication via NmsId ensures NOTAMs returned by both queries appear only once.
    /// </summary>
    private async Task<List<NotamDto>> FindNotamsForBriefingAsync(
        PreflightApiDbContext context, LineString routeLine, double corridorMeters,
        List<string> waypointAirportIdents, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var seenIds = new HashSet<string>();
        var results = new List<NotamDto>();

        // 1. Airport NOTAMs: fetch by location for each waypoint airport
        if (waypointAirportIdents.Count > 0)
        {
            var airportNotams = await context.Notams
                .AsNoTracking()
                .Where(n => (n.Location != null && waypointAirportIdents.Contains(n.Location))
                         || (n.IcaoLocation != null && waypointAirportIdents.Contains(n.IcaoLocation)))
                .Where(n => n.CancelationDate == null || n.CancelationDate > now)
                .Where(n => n.EffectiveEnd == null || n.EffectiveEnd > now)
                .ToListAsync(ct);

            foreach (var entity in airportNotams)
            {
                if (seenIds.Add(entity.NmsId))
                {
                    var dto = DeserializeNotam(entity);
                    if (dto != null) results.Add(dto);
                }
            }
        }

        // 2. En-route NOTAMs: all NOTAMs with geometry within the corridor
        var enrouteNotams = await context.Notams
            .FromSqlInterpolated($"SELECT * FROM notams WHERE geometry IS NOT NULL AND ST_DWithin(geometry::geography, {routeLine}::geography, {corridorMeters})")
            .AsNoTracking()
            .Where(n => n.CancelationDate == null || n.CancelationDate > now)
            .Where(n => n.EffectiveEnd == null || n.EffectiveEnd > now)
            .ToListAsync(ct);

        foreach (var entity in enrouteNotams)
        {
            if (seenIds.Add(entity.NmsId))
            {
                var dto = DeserializeNotam(entity);
                if (dto != null) results.Add(dto);
            }
        }

        return results;
    }

    private static bool IsAirportWaypoint(BriefingWaypoint wp) =>
        !string.IsNullOrWhiteSpace(wp.AirportIdentifier);

    private NotamDto? DeserializeNotam(Domain.Entities.Notam entity)
    {
        try
        {
            return JsonSerializer.Deserialize<NotamDto>(entity.FeatureJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize NOTAM {NmsId}", entity.NmsId);
            return null;
        }
    }
}

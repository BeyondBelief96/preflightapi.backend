using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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

namespace PreflightApi.Infrastructure.Services;

public class BriefingService : IBriefingService
{
    private readonly PreflightApiDbContext _context;
    private readonly IAirportService _airportService;
    private readonly IMetarService _metarService;
    private readonly ITafService _tafService;
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
        IMetarService metarService,
        ITafService tafService,
        ILogger<BriefingService> logger)
    {
        _context = context;
        _airportService = airportService;
        _metarService = metarService;
        _tafService = tafService;
        _logger = logger;
        _geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
    }

    public async Task<RouteBriefingResponse> GetRouteBriefingAsync(RouteBriefingRequest request, CancellationToken ct = default)
    {
        ValidateRequest(request);

        // 1. Resolve all waypoints to coordinates
        var resolvedCoords = await ResolveWaypointsAsync(request.Waypoints, ct);

        // 2. Build route line and corridor
        var routeLine = BuildRouteLineString(resolvedCoords);
        var corridorMeters = request.CorridorWidthNm * NAUTICAL_MILES_TO_METERS;
        var routeDescription = BuildRouteDescription(request.Waypoints);

        _logger.LogInformation(
            "Generating route briefing for {Route} with {CorridorNm} NM corridor",
            routeDescription, request.CorridorWidthNm);

        // 3. Collect airport identifiers from waypoints (for airport-specific NOTAMs)
        var waypointAirportIdents = request.Waypoints
            .Where(w => w.IsAirport)
            .Select(w => w.AirportIdentifier!.ToUpperInvariant().Trim())
            .Distinct()
            .ToList();

        // 4. Spatial queries (sequential — DbContext is not thread-safe)
        var airports = await FindAirportsAlongRouteAsync(routeLine, corridorMeters, ct);
        var pireps = await FindPirepsAlongRouteAsync(routeLine, corridorMeters, ct);
        var sigmets = await FindSigmetsIntersectingRouteAsync(routeLine, ct);
        var gairmets = await FindGAirmetsIntersectingRouteAsync(routeLine, ct);
        var notams = await FindNotamsForBriefingAsync(routeLine, corridorMeters, waypointAirportIdents, ct);

        // 4. Phase 2: Fetch METARs and TAFs for airports found in corridor
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
            metars = (await _metarService.GetMetarsForAirports(airportIdents)).ToList();
            tafs = (await _tafService.GetTafsForAirports(airportIdents)).ToList();
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
            if (wp.IsAirport) continue;

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
        var resolved = new List<(double lon, double lat, string label)>();

        foreach (var wp in waypoints)
        {
            if (wp.IsAirport)
            {
                var airport = await _airportService.GetAirportByIcaoCodeOrIdent(wp.AirportIdentifier!);
                if (airport.LatDecimal == null || airport.LongDecimal == null)
                    throw new ValidationException("waypoints",
                        $"Airport '{wp.AirportIdentifier}' does not have coordinates on record");

                resolved.Add(((double)airport.LongDecimal.Value, (double)airport.LatDecimal.Value,
                    airport.IcaoId ?? airport.ArptId ?? wp.AirportIdentifier!));
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
            wp.IsAirport
                ? wp.AirportIdentifier!.ToUpperInvariant()
                : $"{wp.Latitude:F4},{wp.Longitude:F4}"));
    }

    private async Task<List<AirportDto>> FindAirportsAlongRouteAsync(
        LineString routeLine, double corridorMeters, CancellationToken ct)
    {
        var airports = await _context.Airports
            .AsNoTracking()
            .Where(a => a.Location != null && a.Location.IsWithinDistance(routeLine, corridorMeters))
            .ToListAsync(ct);

        return airports.Select(AirportMapper.ToDto).ToList();
    }

    private async Task<List<PirepDto>> FindPirepsAlongRouteAsync(
        LineString routeLine, double corridorMeters, CancellationToken ct)
    {
        var pireps = await _context.Pireps
            .AsNoTracking()
            .Where(p => p.Location != null && p.Location.IsWithinDistance(routeLine, corridorMeters))
            .ToListAsync(ct);

        return pireps.Select(PirepMapper.ToDto).ToList();
    }

    private async Task<List<SigmetDto>> FindSigmetsIntersectingRouteAsync(
        LineString routeLine, CancellationToken ct)
    {
        var sigmets = await _context.Sigmets
            .AsNoTracking()
            .Where(s => s.Boundary != null && s.Boundary.Intersects(routeLine))
            .ToListAsync(ct);

        return sigmets.Select(SigmetMapper.ToDto).ToList();
    }

    private async Task<List<GAirmetDto>> FindGAirmetsIntersectingRouteAsync(
        LineString routeLine, CancellationToken ct)
    {
        var gairmets = await _context.GAirmets
            .AsNoTracking()
            .Where(g => g.Boundary != null && g.Boundary.Intersects(routeLine))
            .ToListAsync(ct);

        return gairmets.Select(GAirmetMapper.ToDto).ToList();
    }

    /// <summary>
    /// Fetches NOTAMs in two parts mirroring a real preflight briefing:
    /// 1. Airport NOTAMs — by location identifier for the waypoint airports only
    /// 2. En-route NOTAMs — all NOTAMs with geometry within the spatial corridor (TFRs, airspace, etc.)
    /// Deduplication via NmsId ensures NOTAMs returned by both queries appear only once.
    /// </summary>
    private async Task<List<NotamDto>> FindNotamsForBriefingAsync(
        LineString routeLine, double corridorMeters,
        List<string> waypointAirportIdents, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var seenIds = new HashSet<string>();
        var results = new List<NotamDto>();

        // 1. Airport NOTAMs: fetch by location for each waypoint airport
        if (waypointAirportIdents.Count > 0)
        {
            var airportNotams = await _context.Notams
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
        //    Notam.Geometry is geometry(Geometry, 4326), so ST_DWithin uses degrees by default.
        //    Cast to geography so the distance parameter is interpreted in meters.
        //    Dedup with seenIds ensures airport NOTAMs from step 1 aren't repeated.
        var enrouteNotams = await _context.Notams
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

    private static NotamDto? DeserializeNotam(Domain.Entities.Notam entity)
    {
        try
        {
            return JsonSerializer.Deserialize<NotamDto>(entity.FeatureJson, JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}

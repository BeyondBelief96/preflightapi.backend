using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos.Notam;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Settings;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.Services.NotamServices;

/// <summary>
/// NOTAM service backed by the local database (synced via background cron jobs).
/// </summary>
public class NotamService : INotamService
{
    private readonly PreflightApiDbContext _dbContext;
    private readonly NmsSettings _settings;
    private readonly ILogger<NotamService> _logger;

    private static readonly GeometryFactory GeometryFactory =
        new(new PrecisionModel(), 4326);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public NotamService(
        PreflightApiDbContext dbContext,
        IOptions<NmsSettings> settings,
        ILogger<NotamService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<NotamResponseDto> GetNotamsForAirportAsync(string icaoCodeOrIdent, NotamFilterDto? filters = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(icaoCodeOrIdent))
        {
            throw new ArgumentException("Airport identifier cannot be null or empty", nameof(icaoCodeOrIdent));
        }

        var normalizedIdent = icaoCodeOrIdent.ToUpperInvariant().Trim();

        _logger.LogInformation("Getting NOTAMs for airport: {Identifier}", normalizedIdent);

        var query = _dbContext.Notams.AsNoTracking()
            .Where(n => n.Location == normalizedIdent || n.IcaoLocation == normalizedIdent);

        query = ApplyActiveFilter(query);
        query = ApplyFilters(query, filters);

        var entities = await query.ToListAsync(ct);
        var notams = entities.Select(DeserializeFeature).Where(n => n != null).Cast<NotamDto>().ToList();

        return new NotamResponseDto
        {
            Notams = notams,
            TotalCount = notams.Count,
            RetrievedAt = DateTime.UtcNow,
            QueryLocation = normalizedIdent
        };
    }

    public async Task<NotamResponseDto> GetNotamsByRadiusAsync(double lat, double lon, double radiusNm, NotamFilterDto? filters = null, CancellationToken ct = default)
    {
        if (radiusNm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radiusNm), "Radius must be greater than 0");
        }

        if (radiusNm > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(radiusNm), "Radius cannot exceed 100 nautical miles");
        }

        _logger.LogInformation("Getting NOTAMs within {Radius}nm of {Lat}, {Lon}", radiusNm, lat, lon);

        var point = GeometryFactory.CreatePoint(new Coordinate(lon, lat));
        var radiusMeters = radiusNm * 1852.0;

        // Notam.Geometry is geometry(Geometry, 4326), so ST_DWithin uses degrees by default.
        // Cast to geography so the distance parameter is interpreted in meters.
        var query = _dbContext.Notams
            .FromSqlInterpolated($"SELECT * FROM notams WHERE geometry IS NOT NULL AND ST_DWithin(geometry::geography, {point}::geography, {radiusMeters})")
            .AsNoTracking();

        query = ApplyActiveFilter(query);
        query = ApplyFilters(query, filters);

        var entities = await query.ToListAsync(ct);
        var notams = entities.Select(DeserializeFeature).Where(n => n != null).Cast<NotamDto>().ToList();

        return new NotamResponseDto
        {
            Notams = notams,
            TotalCount = notams.Count,
            RetrievedAt = DateTime.UtcNow,
            QueryLocation = $"{lat:F4},{lon:F4} ({radiusNm}nm)"
        };
    }

    public async Task<NotamResponseDto> GetNotamsForRouteAsync(NotamQueryByRouteRequest request, CancellationToken ct = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        // RoutePoints takes precedence if provided; otherwise fall back to AirportIdentifiers
        if (request.RoutePoints is { Count: > 0 })
        {
            return await GetNotamsForRoutePointsAsync(request, ct);
        }

        // Legacy mode: use AirportIdentifiers
        if (request.AirportIdentifiers == null || request.AirportIdentifiers.Count == 0)
        {
            throw new ArgumentException("At least one airport identifier or route point is required", nameof(request));
        }

        return await GetNotamsForAirportIdentifiersAsync(request, ct);
    }

    public async Task<NotamDto?> GetNotamByNmsIdAsync(string nmsId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nmsId))
        {
            throw new ArgumentException("NMS ID cannot be null or empty", nameof(nmsId));
        }

        _logger.LogInformation("Getting NOTAM by NMS ID: {NmsId}", nmsId);

        var entity = await _dbContext.Notams.AsNoTracking()
            .FirstOrDefaultAsync(n => n.NmsId == nmsId, ct);

        return entity != null ? DeserializeFeature(entity) : null;
    }

    public async Task<List<NotamDto>> GetNotamsByNumberAsync(string notamNumber, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(notamNumber))
        {
            throw new ArgumentException("NOTAM number cannot be null or empty", nameof(notamNumber));
        }

        var parsed = NotamNumberParser.Parse(notamNumber);
        if (parsed == null)
        {
            throw new ArgumentException($"Could not parse NOTAM number: '{notamNumber}'", nameof(notamNumber));
        }

        _logger.LogInformation("Searching NOTAMs by number: {Number}, Year: {Year}, Series: {Series}, AccountId: {AccountId}, Location: {Location}",
            parsed.Number, parsed.Year, parsed.Series, parsed.AccountId, parsed.Location);

        var query = _dbContext.Notams.AsNoTracking()
            .Where(n => n.NotamNumber == parsed.Number);

        if (parsed.Year != null)
        {
            query = query.Where(n => n.NotamYear == parsed.Year);
        }

        if (parsed.Series != null)
        {
            query = query.Where(n => n.Series == parsed.Series);
        }

        if (parsed.AccountId != null)
        {
            query = query.Where(n => n.AccountId == parsed.AccountId);
        }

        if (parsed.Location != null)
        {
            var loc = parsed.Location;
            query = query.Where(n => n.Location == loc || n.IcaoLocation == loc);
        }

        var entities = await query.ToListAsync(ct);
        return entities.Select(DeserializeFeature).Where(n => n != null).Cast<NotamDto>().ToList();
    }

    public async Task<PaginatedResponse<NotamDto>> SearchNotamsAsync(NotamFilterDto filters, string? cursor = null, int limit = 100, CancellationToken ct = default)
    {
        if (filters == null || !filters.HasFilters)
        {
            throw new ArgumentException("At least one filter must be provided", nameof(filters));
        }

        _logger.LogInformation("Searching NOTAMs with filters: Classification={Classification}, Feature={Feature}, FreeText={FreeText}",
            filters.Classification, filters.Feature, filters.FreeText);

        IQueryable<Domain.Entities.Notam> query;

        // When lat/lon/radius are provided, start with a spatial query (requires PostGIS)
        if (filters.Latitude.HasValue && filters.Longitude.HasValue && filters.Radius.HasValue)
        {
            var point = GeometryFactory.CreatePoint(new Coordinate(filters.Longitude.Value, filters.Latitude.Value));
            var radiusMeters = filters.Radius.Value * 1852.0;

            query = _dbContext.Notams
                .FromSqlInterpolated($"SELECT * FROM notams WHERE geometry IS NOT NULL AND ST_DWithin(geometry::geography, {point}::geography, {radiusMeters})")
                .AsNoTracking();
        }
        else
        {
            query = _dbContext.Notams.AsNoTracking();
        }

        // LastUpdatedDate: skip active filter and return NOTAMs modified since that date (FAA behavior)
        if (!string.IsNullOrEmpty(filters.LastUpdatedDate) &&
            DateTime.TryParse(filters.LastUpdatedDate, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var lastUpdated))
        {
            query = query.Where(n => n.LastUpdated >= lastUpdated);
        }
        else
        {
            query = ApplyActiveFilter(query);
        }

        query = ApplyFilters(query, filters);

        return await query.ToPaginatedAsync(
            n => n.NmsId,
            n => DeserializeFeature(n)!,
            cursor,
            limit);
    }

    private async Task<NotamResponseDto> GetNotamsForRoutePointsAsync(NotamQueryByRouteRequest request, CancellationToken ct)
    {
        ValidateRoutePoints(request.RoutePoints, request.CorridorRadiusNm);

        _logger.LogInformation("Getting NOTAMs for route with {Count} route points", request.RoutePoints.Count);

        var allNotams = new List<NotamDto>();
        var seenIds = new HashSet<string>();
        var routeDescriptions = new List<string>();

        // Fetch NOTAMs for each route point sequentially (DbContext is not thread-safe)
        foreach (var point in request.RoutePoints)
        {
            routeDescriptions.Add(FormatRoutePointDescription(point));

            List<NotamDto> notams;
            try
            {
                if (IsAirportPoint(point))
                {
                    var response = await GetNotamsForAirportAsync(point.AirportIdentifier!, request.Filters, ct);
                    notams = response.Notams;
                }
                else
                {
                    var radius = GetWaypointRadius(point, request.CorridorRadiusNm);
                    var response = await GetNotamsByRadiusAsync(point.Latitude!.Value, point.Longitude!.Value, radius, request.Filters, ct);
                    notams = response.Notams;
                }
            }
            catch (Exception ex)
            {
                var pointDesc = FormatRoutePointDescription(point);
                _logger.LogWarning(ex, "Failed to fetch NOTAMs for route point {PointDescription}", pointDesc);
                continue;
            }

            foreach (var notam in notams)
            {
                var notamId = GetNotamUniqueId(notam);
                if (!string.IsNullOrEmpty(notamId) && seenIds.Add(notamId))
                {
                    allNotams.Add(notam);
                }
                else if (string.IsNullOrEmpty(notamId))
                {
                    allNotams.Add(notam);
                }
            }
        }

        return new NotamResponseDto
        {
            Notams = allNotams,
            TotalCount = allNotams.Count,
            RetrievedAt = DateTime.UtcNow,
            QueryLocation = string.Join(" -> ", routeDescriptions)
        };
    }

    private async Task<NotamResponseDto> GetNotamsForAirportIdentifiersAsync(NotamQueryByRouteRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Getting NOTAMs for route with {Count} airports", request.AirportIdentifiers.Count);

        var allNotams = new List<NotamDto>();
        var seenIds = new HashSet<string>();

        // Fetch NOTAMs for each airport sequentially (DbContext is not thread-safe)
        foreach (var ident in request.AirportIdentifiers)
        {
            List<NotamDto> notams;
            try
            {
                var response = await GetNotamsForAirportAsync(ident, request.Filters, ct);
                notams = response.Notams;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch NOTAMs for airport {Identifier}", ident);
                continue;
            }

            foreach (var notam in notams)
            {
                var notamId = GetNotamUniqueId(notam);
                if (!string.IsNullOrEmpty(notamId) && seenIds.Add(notamId))
                {
                    allNotams.Add(notam);
                }
                else if (string.IsNullOrEmpty(notamId))
                {
                    allNotams.Add(notam);
                }
            }
        }

        var routeDescription = string.Join(" -> ", request.AirportIdentifiers.Select(i => i.ToUpperInvariant()));

        return new NotamResponseDto
        {
            Notams = allNotams,
            TotalCount = allNotams.Count,
            RetrievedAt = DateTime.UtcNow,
            QueryLocation = routeDescription
        };
    }

    private static IQueryable<Domain.Entities.Notam> ApplyActiveFilter(IQueryable<Domain.Entities.Notam> query)
    {
        var now = DateTime.UtcNow;
        return query
            .Where(n => n.CancelationDate == null || n.CancelationDate > now) // Not manually cancelled
            .Where(n => n.EffectiveEnd == null || n.EffectiveEnd > now);      // Not expired (null = PERM)
    }

    private static IQueryable<Domain.Entities.Notam> ApplyFilters(IQueryable<Domain.Entities.Notam> query, NotamFilterDto? filters)
    {
        if (filters == null || !filters.HasFilters)
            return query;

        if (!string.IsNullOrEmpty(filters.Classification))
        {
            query = query.Where(n => n.Classification == filters.Classification);
        }

        if (!string.IsNullOrEmpty(filters.Feature))
        {
            var feature = filters.Feature.ToUpperInvariant();
            var containment = JsonSerializer.Serialize(new
            {
                properties = new { coreNOTAMData = new { notam = new { feature } } }
            });
            query = query.Where(n => EF.Functions.JsonContains(n.FeatureJson, containment));
        }

        if (!string.IsNullOrEmpty(filters.FreeText))
        {
            var searchText = filters.FreeText;
            query = query.Where(n => n.Text != null && EF.Functions.ILike(n.Text, $"%{searchText}%"));
        }

        if (!string.IsNullOrEmpty(filters.EffectiveStartDate) &&
            DateTime.TryParse(filters.EffectiveStartDate, out var startDate))
        {
            query = query.Where(n => n.EffectiveStart >= startDate);
        }

        if (!string.IsNullOrEmpty(filters.EffectiveEndDate) &&
            DateTime.TryParse(filters.EffectiveEndDate, out var endDate))
        {
            query = query.Where(n => n.EffectiveEnd <= endDate);
        }

        if (!string.IsNullOrEmpty(filters.Accountability))
        {
            var accountability = filters.Accountability.ToUpperInvariant();
            query = query.Where(n => n.AccountId == accountability);
        }

        if (!string.IsNullOrEmpty(filters.Location))
        {
            var location = filters.Location.ToUpperInvariant();
            query = query.Where(n => n.Location == location || n.IcaoLocation == location);
        }

        if (!string.IsNullOrEmpty(filters.NotamNumber))
        {
            var parsed = NotamNumberParser.Parse(filters.NotamNumber);
            if (parsed != null)
            {
                query = query.Where(n => n.NotamNumber == parsed.Number);

                if (parsed.Year != null)
                    query = query.Where(n => n.NotamYear == parsed.Year);

                if (parsed.Series != null)
                    query = query.Where(n => n.Series == parsed.Series);

                if (parsed.AccountId != null)
                    query = query.Where(n => n.AccountId == parsed.AccountId);

                if (parsed.Location != null)
                {
                    var loc = parsed.Location;
                    query = query.Where(n => n.Location == loc || n.IcaoLocation == loc);
                }
            }
        }

        return query;
    }

    private static NotamDto? DeserializeFeature(Domain.Entities.Notam entity)
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

    private void ValidateRoutePoints(List<RoutePointDto> routePoints, double? corridorRadiusNm)
    {
        for (var i = 0; i < routePoints.Count; i++)
        {
            var point = routePoints[i];

            if (IsAirportPoint(point))
            {
                if (string.IsNullOrWhiteSpace(point.AirportIdentifier))
                {
                    throw new ArgumentException($"Route point {i + 1}: Airport identifier cannot be empty");
                }
            }
            else
            {
                if (!point.Latitude.HasValue || !point.Longitude.HasValue)
                {
                    throw new ArgumentException($"Route point {i + 1}: Waypoints require both latitude and longitude");
                }

                if (point.Latitude.Value < -90 || point.Latitude.Value > 90)
                {
                    throw new ArgumentException($"Route point {i + 1}: Latitude must be between -90 and 90 degrees");
                }

                if (point.Longitude.Value < -180 || point.Longitude.Value > 180)
                {
                    throw new ArgumentException($"Route point {i + 1}: Longitude must be between -180 and 180 degrees");
                }

                if (point.RadiusNm.HasValue)
                {
                    if (point.RadiusNm.Value <= 0)
                    {
                        throw new ArgumentException($"Route point {i + 1}: Radius must be greater than 0");
                    }

                    if (point.RadiusNm.Value > 100)
                    {
                        throw new ArgumentException($"Route point {i + 1}: Radius cannot exceed 100 nautical miles");
                    }
                }
            }
        }
    }

    private double GetWaypointRadius(RoutePointDto point, double? corridorRadiusNm)
    {
        return point.RadiusNm ?? corridorRadiusNm ?? _settings.DefaultRouteCorridorRadiusNm;
    }

    private static string FormatRoutePointDescription(RoutePointDto point)
    {
        if (IsAirportPoint(point))
        {
            return point.AirportIdentifier!.ToUpperInvariant();
        }

        if (!string.IsNullOrWhiteSpace(point.Name))
        {
            return point.Name;
        }

        var lat = point.Latitude!.Value;
        var lon = point.Longitude!.Value;
        var latDir = lat >= 0 ? "N" : "S";
        var lonDir = lon >= 0 ? "E" : "W";

        return $"{Math.Abs(lat):F4}{latDir}, {Math.Abs(lon):F4}{lonDir}";
    }

    private static bool IsAirportPoint(RoutePointDto point) =>
        !string.IsNullOrWhiteSpace(point.AirportIdentifier);

    private static string? GetNotamUniqueId(NotamDto notam)
    {
        if (!string.IsNullOrEmpty(notam.Id))
        {
            return notam.Id;
        }

        return notam.Properties?.CoreNotamData?.Notam?.Id;
    }
}

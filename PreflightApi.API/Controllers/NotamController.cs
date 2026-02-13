using System.Text.RegularExpressions;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos.Notam;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

/// <summary>
/// Provides access to NOTAMs (Notices to Air Missions) from the FAA NOTAM Management System (NMS).
/// NOTAMs contain time-critical aeronautical information about airport closures, airspace restrictions,
/// runway conditions, navigation aid outages, and other flight safety hazards.
/// Query by airport identifier, geographic radius, along a flight route, or search across all active NOTAMs.
/// Each NOTAM is returned as a GeoJSON Feature with geographic geometry and detailed properties
/// including effective dates, classification, text content, and plain-English translations.
/// </summary>
/// <remarks>
/// NOTAM data is synced from the FAA NMS system every 3 minutes via background delta sync,
/// with a full refresh daily.
/// Expired NOTAMs (effective end in the past) and cancelled NOTAMs (cancellation date in the past)
/// are automatically excluded from query results
/// (except <c>GET id/{nmsId}</c>, which returns any NOTAM regardless of status).
/// Permanent NOTAMs (no expiration date) remain active indefinitely until manually cancelled.
/// </remarks>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/notams")]
[Tags("NOTAMs")]
public class NotamController(INotamService notamService)
    : ControllerBase
{
    private static readonly HashSet<string> ValidClassifications = new(StringComparer.OrdinalIgnoreCase)
    {
        "INTERNATIONAL", "MILITARY", "LOCAL_MILITARY", "DOMESTIC", "FDC"
    };

    private static readonly HashSet<string> ValidFeatures = new(StringComparer.OrdinalIgnoreCase)
    {
        "RWY", "TWY", "APRON", "AD", "OBST", "NAV", "COM", "SVC", "AIRSPACE",
        "ODP", "SID", "STAR", "CHART", "DATA", "DVA", "IAP", "VFP", "ROUTE", "SPECIAL", "SECURITY"
    };

    private static readonly Regex FreeTextPattern = new(@"^[ /\.\-\(\)\w]{1,80}$", RegexOptions.Compiled);
    private static readonly Regex NmsIdPattern = new(@"^\d{1,64}$", RegexOptions.Compiled);

    /// <summary>
    /// Gets all active NOTAMs for a specific airport
    /// </summary>
    /// <remarks>
    /// Returns NOTAMs matching the airport's FAA identifier or ICAO code. The identifier is
    /// case-insensitive — <c>kdfw</c>, <c>KDFW</c>, and <c>DFW</c> all match the same airport.
    /// Optional filters can narrow results by classification, feature type, text content, or effective date range.
    /// <code>
    /// GET /api/v1/notams/KDFW                                  — all active NOTAMs for DFW
    /// GET /api/v1/notams/DFW?classification=FDC                — only FDC NOTAMs
    /// GET /api/v1/notams/KDFW?feature=RWY                      — only runway-related NOTAMs
    /// GET /api/v1/notams/KDFW?freeText=CLOSED                  — text search within NOTAM text
    /// GET /api/v1/notams/KDFW?classification=DOMESTIC&amp;feature=RWY — combined filters
    /// </code>
    /// </remarks>
    /// <param name="icaoCodeOrIdent">ICAO code (e.g., KDFW) or FAA identifier (e.g., DFW). Case-insensitive.</param>
    /// <param name="classification">Optional NOTAM classification filter: INTERNATIONAL, MILITARY, LOCAL_MILITARY, DOMESTIC, FDC</param>
    /// <param name="feature">Optional NOTAM feature type filter: RWY, TWY, APRON, AD, OBST, NAV, COM, SVC, AIRSPACE, ODP, SID, STAR, CHART, DATA, DVA, IAP, VFP, ROUTE, SPECIAL, SECURITY</param>
    /// <param name="freeText">Optional text search within NOTAM text (max 80 characters, alphanumeric and /.-() only)</param>
    /// <param name="effectiveStartDate">Optional effective start date filter (ISO 8601). Must be paired with effectiveEndDate.</param>
    /// <param name="effectiveEndDate">Optional effective end date filter (ISO 8601). Must be paired with effectiveStartDate.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>All active NOTAMs for the specified airport</returns>
    /// <response code="200">Returns the NOTAMs for the airport</response>
    /// <response code="400">If the airport identifier is missing or filter values are invalid</response>
    [HttpGet("{icaoCodeOrIdent}")]
    [ProducesResponseType(typeof(NotamResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<NotamResponseDto>> GetNotamsForAirport(
        string icaoCodeOrIdent,
        [FromQuery] string? classification,
        [FromQuery] string? feature,
        [FromQuery] string? freeText,
        [FromQuery] string? effectiveStartDate,
        [FromQuery] string? effectiveEndDate,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(icaoCodeOrIdent))
        {
            throw new ValidationException("icaoCodeOrIdent", "Airport identifier is required");
        }

        var filters = BuildFilters(classification, feature, freeText, effectiveStartDate, effectiveEndDate);
        ValidateFilters(filters);

        var result = await notamService.GetNotamsForAirportAsync(icaoCodeOrIdent, filters, ct);
        return Ok(result);
    }

    /// <summary>
    /// Gets NOTAMs within a radius of a geographic point
    /// </summary>
    /// <remarks>
    /// Performs a spatial query using PostGIS to find NOTAMs whose geometry falls within the
    /// specified radius of the given coordinates. Only NOTAMs with stored geometry are returned.
    /// <code>
    /// GET /api/v1/notams/radius?latitude=32.8998&amp;longitude=-97.0403&amp;radiusNm=25
    /// GET /api/v1/notams/radius?latitude=32.8998&amp;longitude=-97.0403&amp;radiusNm=10&amp;classification=DOMESTIC
    /// </code>
    /// </remarks>
    /// <param name="latitude">Latitude in decimal degrees (-90 to 90)</param>
    /// <param name="longitude">Longitude in decimal degrees (-180 to 180)</param>
    /// <param name="radiusNm">Search radius in nautical miles (greater than 0, max 100)</param>
    /// <param name="classification">Optional NOTAM classification filter: INTERNATIONAL, MILITARY, LOCAL_MILITARY, DOMESTIC, FDC</param>
    /// <param name="feature">Optional NOTAM feature type filter: RWY, TWY, APRON, AD, OBST, NAV, COM, SVC, AIRSPACE, ODP, SID, STAR, CHART, DATA, DVA, IAP, VFP, ROUTE, SPECIAL, SECURITY</param>
    /// <param name="freeText">Optional text search within NOTAM text (max 80 characters, alphanumeric and /.-() only)</param>
    /// <param name="effectiveStartDate">Optional effective start date filter (ISO 8601). Must be paired with effectiveEndDate.</param>
    /// <param name="effectiveEndDate">Optional effective end date filter (ISO 8601). Must be paired with effectiveStartDate.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>NOTAMs with geometry within the specified radius</returns>
    /// <response code="200">Returns the NOTAMs within the search radius</response>
    /// <response code="400">If coordinates, radius, or filter values are invalid</response>
    [HttpGet("radius")]
    [ProducesResponseType(typeof(NotamResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<NotamResponseDto>> GetNotamsByRadius(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] double radiusNm,
        [FromQuery] string? classification,
        [FromQuery] string? feature,
        [FromQuery] string? freeText,
        [FromQuery] string? effectiveStartDate,
        [FromQuery] string? effectiveEndDate,
        CancellationToken ct)
    {
        if (latitude < -90 || latitude > 90)
        {
            throw new ValidationException("latitude", "Latitude must be between -90 and 90 degrees");
        }

        if (longitude < -180 || longitude > 180)
        {
            throw new ValidationException("longitude", "Longitude must be between -180 and 180 degrees");
        }

        if (radiusNm <= 0 || radiusNm > 100)
        {
            throw new ValidationException("radiusNm", "Radius must be between 0 and 100 nautical miles");
        }

        var filters = BuildFilters(classification, feature, freeText, effectiveStartDate, effectiveEndDate);
        ValidateFilters(filters);

        var result = await notamService.GetNotamsByRadiusAsync(latitude, longitude, radiusNm, filters, ct);
        return Ok(result);
    }

    /// <summary>
    /// Gets NOTAMs for a flight route (airports and/or waypoints)
    /// </summary>
    /// <remarks>
    /// Fetches NOTAMs for each point along a route, deduplicates them, and returns a single combined result.
    /// The route can be specified in two ways:
    ///
    /// **Option 1 — Airport identifiers only** (simple):
    /// <code>
    /// { "airportIdentifiers": ["KDFW", "KAUS"] }
    /// </code>
    ///
    /// **Option 2 — Route points** (airports + waypoints with coordinates):
    /// <code>
    /// {
    ///   "routePoints": [
    ///     { "airportIdentifier": "KDFW" },
    ///     { "name": "Lake Travis", "latitude": 30.4082, "longitude": -97.8538 },
    ///     { "latitude": 30.1, "longitude": -97.6, "radiusNm": 15 },
    ///     { "airportIdentifier": "KAUS" }
    ///   ],
    ///   "corridorRadiusNm": 25,
    ///   "filters": { "classification": "DOMESTIC", "feature": "RWY" }
    /// }
    /// </code>
    ///
    /// If both <c>routePoints</c> and <c>airportIdentifiers</c> are provided, <c>routePoints</c> takes precedence.
    /// Each waypoint uses its own <c>radiusNm</c> if specified, otherwise falls back to <c>corridorRadiusNm</c>,
    /// then to the server default (25 nm). Airport points query by identifier, not radius.
    /// Optional filters narrow results across all route points.
    /// </remarks>
    /// <param name="request">Route query with airport identifiers and/or route points, optional corridor radius, and optional filters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Aggregated and deduplicated NOTAMs for all points along the route</returns>
    /// <response code="200">Returns the combined NOTAMs for the route</response>
    /// <response code="400">If no airports or route points are provided, or if coordinates/filters are invalid</response>
    [HttpPost("route")]
    [ProducesResponseType(typeof(NotamResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<NotamResponseDto>> GetNotamsForRoute(
        [FromBody] NotamQueryByRouteRequest request,
        CancellationToken ct)
    {
        if (request == null)
        {
            throw new ValidationException("request", "Request body is required");
        }

        var hasRoutePoints = request.RoutePoints is { Count: > 0 };
        var hasAirportIdentifiers = request.AirportIdentifiers is { Count: > 0 };

        if (!hasRoutePoints && !hasAirportIdentifiers)
        {
            throw new ValidationException("request", "At least one airport identifier or route point is required");
        }

        // Validate route points if provided
        if (hasRoutePoints)
        {
            ValidateRoutePoints(request.RoutePoints);
        }

        // Validate filters if provided
        ValidateFilters(request.Filters);

        var result = await notamService.GetNotamsForRouteAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Searches NOTAMs across all locations using filter criteria
    /// </summary>
    /// <remarks>
    /// Searches the entire active NOTAM database without requiring a specific airport or location.
    /// At least one filter parameter is required to prevent unbounded queries.
    /// Results are returned with cursor-based pagination — pass the <c>pagination.nextCursor</c>
    /// value from a previous response as the <c>cursor</c> query parameter to retrieve the next page.
    /// <code>
    /// GET /api/v1/notams/search?classification=FDC                           — all active FDC NOTAMs
    /// GET /api/v1/notams/search?freeText=CLOSED&amp;limit=50                     — text search, 50 per page
    /// GET /api/v1/notams/search?feature=RWY&amp;classification=DOMESTIC           — combined filters
    /// GET /api/v1/notams/search?classification=FDC&amp;cursor=ABC123&amp;limit=100   — next page
    /// </code>
    /// </remarks>
    /// <param name="classification">Optional NOTAM classification filter: INTERNATIONAL, MILITARY, LOCAL_MILITARY, DOMESTIC, FDC</param>
    /// <param name="feature">Optional NOTAM feature type filter: RWY, TWY, APRON, AD, OBST, NAV, COM, SVC, AIRSPACE, ODP, SID, STAR, CHART, DATA, DVA, IAP, VFP, ROUTE, SPECIAL, SECURITY</param>
    /// <param name="freeText">Optional text search within NOTAM text (max 80 characters, alphanumeric and /.-() only)</param>
    /// <param name="effectiveStartDate">Optional effective start date filter (ISO 8601). Must be paired with effectiveEndDate.</param>
    /// <param name="effectiveEndDate">Optional effective end date filter (ISO 8601). Must be paired with effectiveStartDate.</param>
    /// <param name="pagination">Cursor-based pagination parameters. <c>cursor</c>: opaque value from a previous response's <c>nextCursor</c>; <c>limit</c>: items per page (1–500, default 100).</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated NOTAMs matching the filter criteria</returns>
    /// <response code="200">Returns the paginated matching NOTAMs</response>
    /// <response code="400">If no filters are provided, or if filter values are invalid</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PaginatedResponse<NotamDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PaginatedResponse<NotamDto>>> SearchNotams(
        [FromQuery] string? classification,
        [FromQuery] string? feature,
        [FromQuery] string? freeText,
        [FromQuery] string? effectiveStartDate,
        [FromQuery] string? effectiveEndDate,
        [FromQuery] PaginationParams pagination,
        CancellationToken ct)
    {
        var filters = BuildFilters(classification, feature, freeText, effectiveStartDate, effectiveEndDate);

        if (filters == null || !filters.HasFilters)
        {
            throw new ValidationException("filters",
                "At least one filter parameter is required (classification, feature, freeText, effectiveStartDate/effectiveEndDate)");
        }

        ValidateFilters(filters);

        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);

        var result = await notamService.SearchNotamsAsync(filters, pagination.Cursor, pagination.Limit, ct);
        return Ok(result);
    }

    /// <summary>
    /// Gets a single NOTAM by its NMS ID
    /// </summary>
    /// <remarks>
    /// Retrieves a specific NOTAM by its FAA NMS identifier. Unlike other NOTAM endpoints,
    /// this does not filter out cancelled or expired NOTAMs — it returns the NOTAM regardless
    /// of its current status, which is useful for looking up referenced or historical NOTAMs.
    /// <code>
    /// GET /api/v1/notams/id/1757609538792382
    /// </code>
    /// </remarks>
    /// <param name="nmsId">Numeric NMS NOTAM identifier (1–64 digits)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The NOTAM GeoJSON Feature with full properties</returns>
    /// <response code="200">Returns the NOTAM</response>
    /// <response code="400">If the NMS ID is not a valid numeric string</response>
    /// <response code="404">If no NOTAM exists with the given NMS ID</response>
    [HttpGet("id/{nmsId}")]
    [ProducesResponseType(typeof(NotamDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<NotamDto>> GetNotamByNmsId(
        string nmsId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(nmsId))
        {
            throw new ValidationException("nmsId", "NMS ID is required");
        }

        if (!NmsIdPattern.IsMatch(nmsId))
        {
            throw new ValidationException("nmsId", "NMS ID must be a numeric string (1-64 digits)");
        }

        var notam = await notamService.GetNotamByNmsIdAsync(nmsId, ct);

        if (notam == null)
        {
            throw new NotamNotFoundException(nmsId);
        }

        return Ok(notam);
    }

    private static NotamFilterDto? BuildFilters(
        string? classification, string? feature, string? freeText,
        string? effectiveStartDate, string? effectiveEndDate)
    {
        if (classification == null && feature == null && freeText == null &&
            effectiveStartDate == null && effectiveEndDate == null)
        {
            return null;
        }

        return new NotamFilterDto
        {
            Classification = classification,
            Feature = feature,
            FreeText = freeText,
            EffectiveStartDate = effectiveStartDate,
            EffectiveEndDate = effectiveEndDate
        };
    }

    private static void ValidateFilters(NotamFilterDto? filters)
    {
        if (filters == null)
            return;

        if (filters.Classification != null && !ValidClassifications.Contains(filters.Classification))
        {
            throw new ValidationException("classification",
                $"Invalid classification '{filters.Classification}'. Valid values: {string.Join(", ", ValidClassifications)}");
        }

        if (filters.Feature != null && !ValidFeatures.Contains(filters.Feature))
        {
            throw new ValidationException("feature",
                $"Invalid feature '{filters.Feature}'. Valid values: {string.Join(", ", ValidFeatures)}");
        }

        if (filters.FreeText != null && !FreeTextPattern.IsMatch(filters.FreeText))
        {
            throw new ValidationException("freeText",
                "Free text must be 1-80 characters and contain only letters, digits, spaces, and /.-()");
        }

        var hasStart = filters.EffectiveStartDate != null;
        var hasEnd = filters.EffectiveEndDate != null;
        if (hasStart != hasEnd)
        {
            throw new ValidationException("effectiveStartDate",
                "effectiveStartDate and effectiveEndDate must both be provided or both omitted");
        }
    }

    private static void ValidateRoutePoints(List<RoutePointDto> routePoints)
    {
        for (var i = 0; i < routePoints.Count; i++)
        {
            var point = routePoints[i];
            var pointNumber = i + 1;

            var hasAirportId = !string.IsNullOrWhiteSpace(point.AirportIdentifier);
            var hasCoordinates = point.Latitude.HasValue && point.Longitude.HasValue;

            // Each point must be either an airport OR a waypoint with coordinates
            if (!hasAirportId && !hasCoordinates)
            {
                throw new ValidationException($"routePoints[{i}]",
                    $"Route point {pointNumber}: Must specify either an airport identifier or latitude/longitude coordinates");
            }

            // Validate coordinates if this is a waypoint
            if (!hasAirportId)
            {
                if (!point.Latitude.HasValue || !point.Longitude.HasValue)
                {
                    throw new ValidationException($"routePoints[{i}]",
                        $"Route point {pointNumber}: Waypoints require both latitude and longitude");
                }

                if (point.Latitude.Value < -90 || point.Latitude.Value > 90)
                {
                    throw new ValidationException($"routePoints[{i}].latitude",
                        $"Route point {pointNumber}: Latitude must be between -90 and 90 degrees");
                }

                if (point.Longitude.Value < -180 || point.Longitude.Value > 180)
                {
                    throw new ValidationException($"routePoints[{i}].longitude",
                        $"Route point {pointNumber}: Longitude must be between -180 and 180 degrees");
                }

                if (point.RadiusNm.HasValue && (point.RadiusNm.Value <= 0 || point.RadiusNm.Value > 100))
                {
                    throw new ValidationException($"routePoints[{i}].radiusNm",
                        $"Route point {pointNumber}: Radius must be between 0 and 100 nautical miles");
                }
            }
        }
    }
}

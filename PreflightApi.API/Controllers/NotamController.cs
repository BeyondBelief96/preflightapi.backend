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
/// Query by airport identifier, geographic radius, along a flight route, by NOTAM number,
/// or search across all active NOTAMs.
/// Each NOTAM is returned as a GeoJSON Feature with geographic geometry and detailed properties
/// including effective dates, classification, text content, and plain-English translations.
/// </summary>
/// <remarks>
/// <para>
/// NOTAM data is synced from the FAA NMS system every 3 minutes via background delta sync,
/// with a full refresh daily. Expired and cancelled NOTAMs are periodically purged from the database.
/// Most endpoints automatically exclude expired and cancelled NOTAMs that have not yet been purged.
/// The <c>GET id/{nmsId}</c> and <c>GET number/{notamNumber}</c> endpoints skip this filter,
/// so they may return recently expired or cancelled NOTAMs that are still awaiting purge.
/// Permanent NOTAMs (no expiration date) remain active indefinitely until manually cancelled.
/// </para>
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
    private static readonly Regex NotamNumberPattern = new(@"^[A-Za-z0-9 /!\-]{1,30}$", RegexOptions.Compiled);
    private static readonly Regex AlphanumericPattern = new(@"^[A-Za-z0-9]{1,10}$", RegexOptions.Compiled);

    /// <summary>
    /// Gets NOTAMs by NOTAM number in various formats.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Searches the database for NOTAMs matching the given number. Unlike most NOTAM endpoints,
    /// this does <em>not</em> filter out cancelled or expired NOTAMs — so results may include
    /// recently expired or cancelled NOTAMs that have not yet been purged.
    /// This is <em>not</em> a historical archive; the database periodically purges stale NOTAMs.
    /// </para>
    ///
    /// <para><strong>Supported Input Formats</strong></para>
    /// <list type="table">
    ///   <listheader><term>Format</term><description>Example</description></listheader>
    ///   <item><term>Bare number</term><description><c>3997</c></description></item>
    ///   <item><term>Number/year</term><description><c>3997/2025</c> or <c>3997/25</c></description></item>
    ///   <item><term>Month-prefix</term><description><c>03/420</c></description></item>
    ///   <item><term>Domestic</term><description><c>BNA 420</c>, <c>BNA 03/420</c>, <c>!BNA 03/420</c></description></item>
    ///   <item><term>FDC</term><description><c>FDC 4/3997</c>, <c>!FDC 4/3997</c></description></item>
    ///   <item><term>ICAO</term><description><c>A1234/25</c></description></item>
    /// </list>
    ///
    /// <para><strong>Disambiguation</strong></para>
    /// <para>
    /// Bare numbers (e.g., <c>3997</c>) may match multiple NOTAMs across different accounts or years.
    /// Include the year, account ID, or full domestic format to narrow results.
    /// </para>
    ///
    /// <para><strong>Examples</strong></para>
    /// <code>
    /// GET /api/v1/notams/number/3997                — bare number (may return multiple matches)
    /// GET /api/v1/notams/number/3997%2F2025          — number with year (%2F = /)
    /// GET /api/v1/notams/number/BNA%20420             — domestic format (%20 = space)
    /// GET /api/v1/notams/number/A1234%2F25            — ICAO format
    /// </code>
    /// </remarks>
    /// <param name="notamNumber">NOTAM number in any supported format (URL-encoded)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of matching NOTAMs as GeoJSON Features</returns>
    /// <response code="200">Returns the matching NOTAMs</response>
    /// <response code="400">If the input cannot be parsed or is invalid</response>
    /// <response code="404">If no NOTAMs match the given number</response>
    [HttpGet("number/{notamNumber}")]
    [ProducesResponseType(typeof(List<NotamDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<NotamDto>>> GetNotamsByNumber(
        string notamNumber,
        CancellationToken ct)
    {
        var decoded = Uri.UnescapeDataString(notamNumber);

        if (string.IsNullOrWhiteSpace(decoded))
        {
            throw new ValidationException("notamNumber", "NOTAM number is required");
        }

        if (!NotamNumberPattern.IsMatch(decoded))
        {
            throw new ValidationException("notamNumber",
                "NOTAM number must be 1-30 characters containing only letters, digits, spaces, /, !, and hyphens");
        }

        var results = await notamService.GetNotamsByNumberAsync(decoded, ct);

        if (results.Count == 0)
        {
            throw new NotamNotFoundException(decoded);
        }

        return Ok(results);
    }

    /// <summary>
    /// Gets all active NOTAMs for a specific airport.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns NOTAMs matching the airport's FAA identifier or ICAO code. The identifier is
    /// case-insensitive — <c>kdfw</c>, <c>KDFW</c>, and <c>DFW</c> all match the same airport.
    /// </para>
    ///
    /// <para><strong>Optional Filters</strong></para>
    /// <para>
    /// All filter parameters are optional and can be combined to narrow results:
    /// </para>
    ///
    /// <list type="bullet">
    ///   <item>
    ///     <description><c>classification</c> — NOTAM classification: <c>INTERNATIONAL</c>, <c>MILITARY</c>, <c>LOCAL_MILITARY</c>, <c>DOMESTIC</c>, <c>FDC</c></description>
    ///   </item>
    ///   <item>
    ///     <description><c>feature</c> — feature type: <c>RWY</c>, <c>TWY</c>, <c>APRON</c>, <c>AD</c>, <c>OBST</c>, <c>NAV</c>, <c>COM</c>, <c>SVC</c>, <c>AIRSPACE</c>, <c>ODP</c>, <c>SID</c>, <c>STAR</c>, <c>CHART</c>, <c>DATA</c>, <c>DVA</c>, <c>IAP</c>, <c>VFP</c>, <c>ROUTE</c>, <c>SPECIAL</c>, <c>SECURITY</c></description>
    ///   </item>
    ///   <item>
    ///     <description><c>freeText</c> — text search within NOTAM text (max 80 characters, alphanumeric and <c>/.-( )</c> only)</description>
    ///   </item>
    ///   <item>
    ///     <description><c>effectiveStartDate</c> / <c>effectiveEndDate</c> — ISO 8601 date range (must be paired)</description>
    ///   </item>
    /// </list>
    ///
    /// <para><strong>Examples</strong></para>
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
    /// Gets NOTAMs within a radius of a geographic point.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Performs a spatial query using PostGIS to find NOTAMs whose geometry falls within the
    /// specified radius of the given coordinates. Only NOTAMs with stored geometry are returned —
    /// NOTAMs that lack geographic data (no point or polygon in the source GeoJSON) are excluded
    /// from spatial queries.
    /// </para>
    ///
    /// <para>
    /// The same optional filters available on the airport endpoint (<c>classification</c>,
    /// <c>feature</c>, <c>freeText</c>, <c>effectiveStartDate</c>/<c>effectiveEndDate</c>)
    /// can be combined with the spatial search.
    /// </para>
    ///
    /// <para><strong>Examples</strong></para>
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
    /// Gets NOTAMs for a flight route (airports and/or waypoints).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Queries NOTAMs for each point along a route, deduplicates them, and returns a single combined result.
    /// </para>
    ///
    /// <para><strong>Option 1 — Airport identifiers only (simple)</strong></para>
    /// <para>
    /// Provide a list of airport identifiers. Each airport is queried by identifier match (FAA or ICAO).
    /// Best for straightforward airport-to-airport routes with no en-route waypoints.
    /// </para>
    /// <code>
    /// { "airportIdentifiers": ["KDFW", "KAUS"] }
    /// </code>
    ///
    /// <para><strong>Option 2 — Route points (airports + waypoints)</strong></para>
    /// <para>
    /// Provide an ordered list of route points. Each point is either an airport (queried by identifier)
    /// or a geographic waypoint (queried by spatial radius around its coordinates).
    /// Use this when your route includes en-route waypoints or you need per-point radius control.
    /// </para>
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
    /// <para><strong>How each point type is queried</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Airport points</strong> — queried by identifier (same as the single-airport endpoint). Radius settings do not apply.</description></item>
    ///   <item><description><strong>Waypoints</strong> — queried by spatial radius. The radius used is: the point's own <c>radiusNm</c> if set, otherwise the request-level <c>corridorRadiusNm</c>, otherwise the server default (25 NM).</description></item>
    /// </list>
    ///
    /// <para><strong>Notes</strong></para>
    /// <list type="bullet">
    ///   <item><description>If both <c>routePoints</c> and <c>airportIdentifiers</c> are provided, <c>routePoints</c> is used and <c>airportIdentifiers</c> is ignored.</description></item>
    ///   <item><description>Duplicate NOTAMs appearing at multiple route points are returned only once.</description></item>
    ///   <item><description>Optional <c>filters</c> (classification, feature, freeText, date range) are applied to every route point query.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="request">Route query — provide either <c>airportIdentifiers</c> or <c>routePoints</c>, with optional <c>corridorRadiusNm</c> and <c>filters</c></param>
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
    /// Searches NOTAMs across all locations using filter criteria.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Searches the entire active NOTAM database without requiring a specific airport or location.
    /// At least one filter parameter is required to prevent unbounded queries.
    /// Mirrors the FAA NMS API query parameters for flexible NOTAM filtering.
    /// </para>
    ///
    /// <para><strong>Pagination</strong></para>
    /// <para>
    /// Results are returned with cursor-based pagination. Pass the <c>pagination.nextCursor</c>
    /// value from a previous response as the <c>cursor</c> query parameter to retrieve the next page.
    /// The <c>limit</c> parameter controls page size (1–500, default 100).
    /// </para>
    ///
    /// <para><strong>Parameter Pairing Rules</strong></para>
    /// <list type="bullet">
    ///   <item><description><c>notamNumber</c> must be paired with <c>location</c> or <c>accountability</c></description></item>
    ///   <item><description><c>latitude</c>, <c>longitude</c>, and <c>radius</c> must all be provided together</description></item>
    ///   <item><description><c>effectiveStartDate</c> and <c>effectiveEndDate</c> must both be provided or both omitted</description></item>
    ///   <item><description><c>lastUpdatedDate</c>: when provided, returns both active and inactive NOTAMs modified since that time</description></item>
    /// </list>
    ///
    /// <para><strong>Examples</strong></para>
    /// <code>
    /// GET /api/v1/notams/search?classification=FDC                                          — all active FDC NOTAMs
    /// GET /api/v1/notams/search?freeText=CLOSED&amp;limit=50                                    — text search, 50 per page
    /// GET /api/v1/notams/search?feature=RWY&amp;classification=DOMESTIC                          — combined filters
    /// GET /api/v1/notams/search?accountability=BNA                                          — NOTAMs by issuing office
    /// GET /api/v1/notams/search?location=DFW                                                — NOTAMs for a location
    /// GET /api/v1/notams/search?notamNumber=420&amp;location=DFW                                 — NOTAM by number + location
    /// GET /api/v1/notams/search?latitude=32.8998&amp;longitude=-97.0403&amp;radius=25                — spatial search
    /// GET /api/v1/notams/search?lastUpdatedDate=2025-03-01T00:00:00Z                        — recently modified (active + inactive)
    /// GET /api/v1/notams/search?classification=FDC&amp;cursor=ABC123&amp;limit=100                   — next page
    /// </code>
    /// </remarks>
    /// <param name="classification">Optional NOTAM classification filter: INTERNATIONAL, MILITARY, LOCAL_MILITARY, DOMESTIC, FDC</param>
    /// <param name="feature">Optional NOTAM feature type filter: RWY, TWY, APRON, AD, OBST, NAV, COM, SVC, AIRSPACE, ODP, SID, STAR, CHART, DATA, DVA, IAP, VFP, ROUTE, SPECIAL, SECURITY</param>
    /// <param name="freeText">Optional text search within NOTAM text (max 80 characters, alphanumeric and /.-() only)</param>
    /// <param name="effectiveStartDate">Optional effective start date filter (ISO 8601). Must be paired with effectiveEndDate.</param>
    /// <param name="effectiveEndDate">Optional effective end date filter (ISO 8601). Must be paired with effectiveStartDate.</param>
    /// <param name="accountability">Optional accountability code (issuing office) filter, e.g., "BNA", "FDC". Alphanumeric, max 10 characters.</param>
    /// <param name="location">Optional location identifier filter (FAA domestic or ICAO code), e.g., "DFW" or "KDFW". Alphanumeric, max 10 characters.</param>
    /// <param name="notamNumber">Optional NOTAM number filter. Must be paired with location or accountability.</param>
    /// <param name="latitude">Optional latitude in decimal degrees (-90 to 90). Must be paired with longitude and radius.</param>
    /// <param name="longitude">Optional longitude in decimal degrees (-180 to 180). Must be paired with latitude and radius.</param>
    /// <param name="radius">Optional search radius in nautical miles (0 to 100). Must be paired with latitude and longitude.</param>
    /// <param name="lastUpdatedDate">Optional ISO 8601 timestamp. Returns NOTAMs modified since this time, including inactive NOTAMs.</param>
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
        [FromQuery] string? accountability,
        [FromQuery] string? location,
        [FromQuery] string? notamNumber,
        [FromQuery] double? latitude,
        [FromQuery] double? longitude,
        [FromQuery] double? radius,
        [FromQuery] string? lastUpdatedDate,
        [FromQuery] PaginationParams pagination,
        CancellationToken ct)
    {
        var filters = BuildFilters(classification, feature, freeText, effectiveStartDate, effectiveEndDate,
            accountability, location, notamNumber, latitude, longitude, radius, lastUpdatedDate);

        if (filters == null || !filters.HasFilters)
        {
            throw new ValidationException("filters",
                "At least one filter parameter is required (classification, feature, freeText, effectiveStartDate/effectiveEndDate, " +
                "accountability, location, notamNumber, latitude/longitude/radius, lastUpdatedDate)");
        }

        ValidateFilters(filters);

        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);

        var result = await notamService.SearchNotamsAsync(filters, pagination.Cursor, pagination.Limit, ct);
        return Ok(result);
    }

    /// <summary>
    /// Gets a single NOTAM by its NMS ID.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Retrieves a specific NOTAM by its FAA NMS identifier. Unlike most NOTAM endpoints,
    /// this does <em>not</em> filter out cancelled or expired NOTAMs — so the result may be
    /// a recently expired or cancelled NOTAM that has not yet been purged.
    /// This is <em>not</em> a historical archive; the database periodically purges stale NOTAMs.
    /// </para>
    ///
    /// <para><strong>Example</strong></para>
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
        return BuildFilters(classification, feature, freeText, effectiveStartDate, effectiveEndDate,
            null, null, null, null, null, null, null);
    }

    private static NotamFilterDto? BuildFilters(
        string? classification, string? feature, string? freeText,
        string? effectiveStartDate, string? effectiveEndDate,
        string? accountability, string? location, string? notamNumber,
        double? latitude, double? longitude, double? radius,
        string? lastUpdatedDate)
    {
        if (classification == null && feature == null && freeText == null &&
            effectiveStartDate == null && effectiveEndDate == null &&
            accountability == null && location == null && notamNumber == null &&
            latitude == null && longitude == null && radius == null &&
            lastUpdatedDate == null)
        {
            return null;
        }

        return new NotamFilterDto
        {
            Classification = classification,
            Feature = feature,
            FreeText = freeText,
            EffectiveStartDate = effectiveStartDate,
            EffectiveEndDate = effectiveEndDate,
            Accountability = accountability,
            Location = location,
            NotamNumber = notamNumber,
            Latitude = latitude,
            Longitude = longitude,
            Radius = radius,
            LastUpdatedDate = lastUpdatedDate
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

        if (filters.Accountability != null && !AlphanumericPattern.IsMatch(filters.Accountability))
        {
            throw new ValidationException("accountability",
                "Accountability must be alphanumeric and at most 10 characters");
        }

        if (filters.Location != null && !AlphanumericPattern.IsMatch(filters.Location))
        {
            throw new ValidationException("location",
                "Location must be alphanumeric and at most 10 characters");
        }

        if (filters.NotamNumber != null)
        {
            if (!NotamNumberPattern.IsMatch(filters.NotamNumber))
            {
                throw new ValidationException("notamNumber",
                    "NOTAM number must be 1-30 characters containing only letters, digits, spaces, /, !, and hyphens");
            }

            if (filters.Location == null && filters.Accountability == null)
            {
                throw new ValidationException("notamNumber",
                    "notamNumber must be paired with location or accountability");
            }
        }

        // Latitude, longitude, radius must all be provided together
        var hasLat = filters.Latitude.HasValue;
        var hasLon = filters.Longitude.HasValue;
        var hasRadius = filters.Radius.HasValue;
        if (hasLat || hasLon || hasRadius)
        {
            if (!hasLat || !hasLon || !hasRadius)
            {
                throw new ValidationException("latitude",
                    "latitude, longitude, and radius must all be provided together");
            }

            if (filters.Latitude!.Value < -90 || filters.Latitude.Value > 90)
            {
                throw new ValidationException("latitude",
                    "Latitude must be between -90 and 90 degrees");
            }

            if (filters.Longitude!.Value < -180 || filters.Longitude.Value > 180)
            {
                throw new ValidationException("longitude",
                    "Longitude must be between -180 and 180 degrees");
            }

            if (filters.Radius!.Value <= 0 || filters.Radius.Value > 100)
            {
                throw new ValidationException("radius",
                    "Radius must be between 0 and 100 nautical miles");
            }
        }

        if (filters.LastUpdatedDate != null && !DateTime.TryParse(filters.LastUpdatedDate, out _))
        {
            throw new ValidationException("lastUpdatedDate",
                "lastUpdatedDate must be a valid ISO 8601 timestamp");
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

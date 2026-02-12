using System.Text.RegularExpressions;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos.Notam;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

/// <summary>
/// Provides access to NOTAMs (Notices to Air Missions) from the FAA NOTAM Search (NMS) system.
/// NOTAMs contain time-critical aeronautical information about airport closures, airspace restrictions,
/// runway conditions, navigation aid outages, and other flight safety hazards. NOTAMs are returned as
/// GeoJSON Features with geographic geometry and detailed properties including effective dates, text content,
/// and plain-English translations. Query by airport, geographic radius, along a flight route, or by NMS ID.
/// </summary>
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
    private static readonly Regex NmsIdPattern = new(@"^\d{16}$", RegexOptions.Compiled);

    /// <summary>
    /// Gets NOTAMs for a specific airport
    /// </summary>
    /// <param name="icaoCodeOrIdent">ICAO code or FAA identifier (e.g., KDFW, DFW)</param>
    /// <param name="classification">NOTAM classification filter (INTERNATIONAL, MILITARY, LOCAL_MILITARY, DOMESTIC, FDC)</param>
    /// <param name="feature">NOTAM feature type filter (RWY, TWY, APRON, AD, OBST, NAV, COM, SVC, AIRSPACE, ODP, SID, STAR, CHART, DATA, DVA, IAP, VFP, ROUTE, SPECIAL, SECURITY)</param>
    /// <param name="freeText">Free text search within NOTAM text (max 80 chars)</param>
    /// <param name="effectiveStartDate">Effective start date filter (ISO 8601). Must be paired with effectiveEndDate.</param>
    /// <param name="effectiveEndDate">Effective end date filter (ISO 8601). Must be paired with effectiveStartDate.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>NOTAMs for the specified airport</returns>
    /// <remarks>
    /// **FAA NMS Usage Limits:** The upstream FAA NOTAM Management System enforces rate limits on data consumers.
    /// Delta queries are limited to 1 request per 3 minutes; bulk pulls to 1 per 24 hours. More frequent use
    /// requires FAA approval and will result in rate limit errors. This API caches NOTAM responses for 5 minutes
    /// to help stay within these limits. These limits are set by the FAA, not by this API.
    /// </remarks>
    /// <response code="200">Returns the NOTAMs</response>
    /// <response code="400">If the identifier or filters are invalid</response>
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
    /// <param name="latitude">Latitude in decimal degrees</param>
    /// <param name="longitude">Longitude in decimal degrees</param>
    /// <param name="radiusNm">Radius in nautical miles (max 100)</param>
    /// <param name="classification">NOTAM classification filter (INTERNATIONAL, MILITARY, LOCAL_MILITARY, DOMESTIC, FDC)</param>
    /// <param name="feature">NOTAM feature type filter (RWY, TWY, APRON, AD, OBST, NAV, COM, SVC, AIRSPACE, ODP, SID, STAR, CHART, DATA, DVA, IAP, VFP, ROUTE, SPECIAL, SECURITY)</param>
    /// <param name="freeText">Free text search within NOTAM text (max 80 chars)</param>
    /// <param name="effectiveStartDate">Effective start date filter (ISO 8601). Must be paired with effectiveEndDate.</param>
    /// <param name="effectiveEndDate">Effective end date filter (ISO 8601). Must be paired with effectiveStartDate.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>NOTAMs within the specified radius</returns>
    /// <remarks>
    /// **FAA NMS Usage Limits:** The upstream FAA NOTAM Management System enforces rate limits on data consumers.
    /// Delta queries are limited to 1 request per 3 minutes; bulk pulls to 1 per 24 hours. More frequent use
    /// requires FAA approval and will result in rate limit errors. This API caches NOTAM responses for 5 minutes
    /// to help stay within these limits. These limits are set by the FAA, not by this API.
    /// </remarks>
    /// <response code="200">Returns the NOTAMs</response>
    /// <response code="400">If parameters or filters are invalid</response>
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
    /// <param name="request">Route query request with airport identifiers and/or route points with coordinates</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Aggregated and deduplicated NOTAMs for the route</returns>
    /// <remarks>
    /// The route can be specified using either:
    /// - AirportIdentifiers: Simple list of airport ICAO/FAA codes (e.g., ["KDFW", "KAUS"])
    /// - RoutePoints: Ordered list of mixed airports and waypoints with coordinates
    ///
    /// If both are provided, RoutePoints takes precedence.
    ///
    /// Optional NMS filters can be included in the request body via the "filters" property.
    ///
    /// Example RoutePoints request:
    /// ```json
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
    /// ```
    ///
    /// **FAA NMS Usage Limits:** The upstream FAA NOTAM Management System enforces rate limits on data consumers.
    /// Delta queries are limited to 1 request per 3 minutes; bulk pulls to 1 per 24 hours. More frequent use
    /// requires FAA approval and will result in rate limit errors. This API caches NOTAM responses for 5 minutes
    /// to help stay within these limits. These limits are set by the FAA, not by this API.
    /// </remarks>
    /// <response code="200">Returns the NOTAMs</response>
    /// <response code="400">If the request is invalid</response>
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
    /// Gets a single NOTAM by its NMS ID
    /// </summary>
    /// <param name="nmsId">16-digit NMS NOTAM identifier</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The NOTAM if found</returns>
    /// <remarks>
    /// **FAA NMS Usage Limits:** The upstream FAA NOTAM Management System enforces rate limits on data consumers.
    /// Delta queries are limited to 1 request per 3 minutes; bulk pulls to 1 per 24 hours. More frequent use
    /// requires FAA approval and will result in rate limit errors. This API caches NOTAM responses for 5 minutes
    /// to help stay within these limits. These limits are set by the FAA, not by this API.
    /// </remarks>
    /// <response code="200">Returns the NOTAM</response>
    /// <response code="400">If the NMS ID format is invalid</response>
    /// <response code="404">If the NOTAM is not found</response>
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
            throw new ValidationException("nmsId", "NMS ID must be a 16-digit number");
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

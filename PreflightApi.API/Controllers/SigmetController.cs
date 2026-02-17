using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Domain.Enums;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

/// <summary>
/// Provides access to domestic SIGMET advisories for the contiguous United States.
/// Does not include SIGMETs issued by the US in international format. SIGMETs warn of severe weather
/// hazards significant to all aircraft (severe turbulence/icing, convective activity). Each advisory includes
/// the hazard type, severity, altitude range, and geographic polygon defining the affected area.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/sigmets")]
[Tags("Weather - Domestic SIGMETs")]
public class SigmetController(ISigmetService sigmetService) : ControllerBase
{
    /// <summary>
    /// Gets all current domestic SIGMET advisories across all hazard types.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns every active domestic SIGMET regardless of hazard type. Each advisory includes the
    /// hazard (convective activity, severe turbulence, severe icing, IFR conditions, or mountain
    /// obscuration), severity, affected altitude range, and a geographic polygon defining the affected area.
    /// Use the <c>GET /hazard/{hazardType}</c> endpoint to filter by a specific hazard type.
    /// </para>
    /// </remarks>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of all active domestic SIGMET advisories with hazard details and geographic boundaries</returns>
    /// <response code="200">Returns the paginated list of all current domestic SIGMETs</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<SigmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<SigmetDto>>> GetAllSigmets(
        [FromQuery] PaginationParams pagination,
        CancellationToken ct)
    {
        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await sigmetService.GetAllSigmets(pagination.Cursor, pagination.Limit, ct));
    }

    /// <summary>
    /// Gets SIGMETs filtered by hazard type.
    /// </summary>
    /// <remarks>
    /// <para><strong>Hazard Types</strong></para>
    /// <list type="bullet">
    ///   <item><description><c>CONVECTIVE</c> — thunderstorms and convective activity</description></item>
    ///   <item><description><c>ICE</c> — severe icing</description></item>
    ///   <item><description><c>TURB</c> — severe turbulence</description></item>
    ///   <item><description><c>IFR</c> — widespread IFR conditions</description></item>
    ///   <item><description><c>MTN_OBSCN</c> — mountain obscuration</description></item>
    /// </list>
    /// <code>
    /// GET /api/v1/sigmets/hazard/CONVECTIVE
    /// GET /api/v1/sigmets/hazard/TURB
    /// </code>
    /// </remarks>
    /// <param name="hazardType">Hazard type: <c>CONVECTIVE</c>, <c>ICE</c>, <c>TURB</c>, <c>IFR</c>, or <c>MTN_OBSCN</c></param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated SIGMETs matching the specified hazard type</returns>
    /// <response code="200">Returns the filtered SIGMETs</response>
    /// <response code="400">If the hazard type is invalid</response>
    [HttpGet("hazard/{hazardType}")]
    [ProducesResponseType(typeof(PaginatedResponse<SigmetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<SigmetDto>>> GetSigmetsByHazardType(
        string hazardType,
        [FromQuery] PaginationParams pagination,
        CancellationToken ct)
    {
        if (!Enum.TryParse<SigmetHazardType>(hazardType, ignoreCase: true, out var hazardTypeEnum))
            throw new ValidationException("hazardType", $"Invalid hazard type '{hazardType}'. Valid values are: CONVECTIVE, ICE, TURB, IFR, MTN_OBSCN");

        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await sigmetService.GetSigmetsByHazardType(hazardTypeEnum, pagination.Cursor, pagination.Limit, ct));
    }

    /// <summary>
    /// Finds SIGMETs whose geographic boundary contains the given point. Returns advisories
    /// that affect a specific location, answering "what SIGMETs are active at this position?"
    /// </summary>
    /// <remarks>
    /// <code>
    /// GET /api/v1/sigmets/affecting?lat=32.897&amp;lon=-97.038
    /// </code>
    /// </remarks>
    /// <param name="lat">Latitude in decimal degrees (-90 to 90)</param>
    /// <param name="lon">Longitude in decimal degrees (-180 to 180)</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of SIGMETs affecting the given point</returns>
    /// <response code="200">Returns the SIGMETs found</response>
    /// <response code="400">If coordinates are invalid</response>
    [HttpGet("affecting")]
    [ProducesResponseType(typeof(PaginatedResponse<SigmetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<SigmetDto>>> SearchAffecting(
        [FromQuery] decimal lat,
        [FromQuery] decimal lon,
        [FromQuery] PaginationParams? pagination = null,
        CancellationToken ct = default)
    {
        if (lat < -90 || lat > 90)
            throw new ValidationException("lat", "Latitude must be between -90 and 90 degrees");
        if (lon < -180 || lon > 180)
            throw new ValidationException("lon", "Longitude must be between -180 and 180 degrees");

        pagination ??= new PaginationParams();
        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await sigmetService.SearchAffecting(lat, lon, pagination.Cursor, pagination.Limit, ct));
    }

    /// <summary>
    /// Finds SIGMETs that intersect a geographic bounding box. Returns advisories whose
    /// boundary overlaps the specified area, useful for checking conditions across a flight route.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Note:</strong> The bounding box must not cross the antimeridian (i.e., <c>minLon</c>
    /// must be less than <c>maxLon</c>). Antimeridian-crossing queries are not supported.
    /// This endpoint covers domestic US SIGMETs within the contiguous United States.
    /// </para>
    /// <code>
    /// GET /api/v1/sigmets/by-area?minLat=30&amp;maxLat=35&amp;minLon=-100&amp;maxLon=-95
    /// </code>
    /// </remarks>
    /// <param name="minLat">Minimum latitude (-90 to 90)</param>
    /// <param name="maxLat">Maximum latitude (-90 to 90)</param>
    /// <param name="minLon">Minimum longitude (-180 to 180)</param>
    /// <param name="maxLon">Maximum longitude (-180 to 180)</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of SIGMETs intersecting the bounding box</returns>
    /// <response code="200">Returns the SIGMETs found</response>
    /// <response code="400">If coordinates are invalid or the bounding box crosses the antimeridian</response>
    [HttpGet("by-area")]
    [ProducesResponseType(typeof(PaginatedResponse<SigmetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<SigmetDto>>> SearchByArea(
        [FromQuery] decimal minLat,
        [FromQuery] decimal maxLat,
        [FromQuery] decimal minLon,
        [FromQuery] decimal maxLon,
        [FromQuery] PaginationParams? pagination = null,
        CancellationToken ct = default)
    {
        if (minLat < -90 || minLat > 90 || maxLat < -90 || maxLat > 90)
            throw new ValidationException("lat", "Latitude values must be between -90 and 90 degrees");
        if (minLon < -180 || minLon > 180 || maxLon < -180 || maxLon > 180)
            throw new ValidationException("lon", "Longitude values must be between -180 and 180 degrees");
        if (minLat >= maxLat)
            throw new ValidationException("lat", "minLat must be less than maxLat");
        if (minLon >= maxLon)
            throw new ValidationException("lon", "minLon must be less than maxLon");

        pagination ??= new PaginationParams();
        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await sigmetService.SearchByArea(minLat, maxLat, minLon, maxLon, pagination.Cursor, pagination.Limit, ct));
    }
}

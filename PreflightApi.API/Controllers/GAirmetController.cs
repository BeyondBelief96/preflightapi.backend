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
/// Provides access to decoded G-AIRMETs (Graphical AIRMETs) for the contiguous United States — graphical weather
/// advisories issued by the Aviation Weather Center. G-AIRMETs are organized by product type: SIERRA
/// (IFR/mountain obscuration), TANGO (turbulence/wind shear/surface winds), and ZULU (icing/freezing level).
/// They can also be queried by specific hazard type (e.g., ICE, TURB_LO, IFR). Each advisory includes the
/// hazard, severity, altitude range, and geographic polygon of the affected area. G-AIRMETs are issued every
/// 3 hours with forecasts at 0, 3, 6, 9, and 12 hour intervals.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/g-airmets")]
[Tags("Weather - G-AIRMETs")]
public class GAirmetController(IGAirmetService gairmetService) : ControllerBase
{
    /// <summary>
    /// Gets all current G-AIRMET advisories across all product types and hazards.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns every active G-AIRMET regardless of product type (SIERRA, TANGO, ZULU) or hazard type.
    /// Each advisory includes the hazard, severity, affected altitude range, forecast valid time,
    /// and a geographic polygon defining the affected area. Use the
    /// <c>GET /product/{product}</c> or <c>GET /hazard/{hazardType}</c> endpoints to filter
    /// by specific product or hazard type.
    /// </para>
    /// </remarks>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of all active G-AIRMET advisories with hazard details and geographic boundaries</returns>
    /// <response code="200">Returns the paginated list of all current G-AIRMETs</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<GAirmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<GAirmetDto>>> GetAllGAirmets(
        [FromQuery] PaginationParams pagination,
        CancellationToken ct)
    {
        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await gairmetService.GetAllGAirmets(pagination.Cursor, pagination.Limit, ct));
    }

    /// <summary>
    /// Gets G-AIRMETs filtered by product type
    /// </summary>
    /// <param name="product">Product type: SIERRA, TANGO, or ZULU</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated G-AIRMETs matching the specified product type</returns>
    /// <response code="200">Returns the filtered G-AIRMETs</response>
    /// <response code="400">If the product type is invalid</response>
    [HttpGet("product/{product}")]
    [ProducesResponseType(typeof(PaginatedResponse<GAirmetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<GAirmetDto>>> GetGAirmetsByProduct(
        string product,
        [FromQuery] PaginationParams pagination,
        CancellationToken ct)
    {
        if (!Enum.TryParse<GAirmetProduct>(product, ignoreCase: true, out var productEnum))
            throw new ValidationException("product", $"Invalid product type '{product}'. Valid values are: SIERRA, TANGO, ZULU");

        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await gairmetService.GetGAirmetsByProduct(productEnum, pagination.Cursor, pagination.Limit, ct));
    }

    /// <summary>
    /// Gets G-AIRMETs filtered by hazard type
    /// </summary>
    /// <param name="hazardType">Hazard type: MT_OBSC, IFR, TURB_LO, TURB_HI, LLWS, SFC_WIND, ICE, FZLVL, or M_FZLVL</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated G-AIRMETs matching the specified hazard type</returns>
    /// <response code="200">Returns the filtered G-AIRMETs</response>
    /// <response code="400">If the hazard type is invalid</response>
    [HttpGet("hazard/{hazardType}")]
    [ProducesResponseType(typeof(PaginatedResponse<GAirmetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<GAirmetDto>>> GetGAirmetsByHazardType(
        string hazardType,
        [FromQuery] PaginationParams pagination,
        CancellationToken ct)
    {
        if (!Enum.TryParse<GAirmetHazardType>(hazardType, ignoreCase: true, out var hazardTypeEnum))
            throw new ValidationException("hazardType", $"Invalid hazard type '{hazardType}'. Valid values are: MT_OBSC, IFR, TURB_LO, TURB_HI, LLWS, SFC_WIND, ICE, FZLVL, M_FZLVL");

        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await gairmetService.GetGAirmetsByHazardType(hazardTypeEnum, pagination.Cursor, pagination.Limit, ct));
    }

    /// <summary>
    /// Finds G-AIRMETs whose geographic boundary contains the given point. Returns advisories
    /// that affect a specific location, answering "what G-AIRMETs are active at this position?"
    /// </summary>
    /// <param name="lat">Latitude in decimal degrees (-90 to 90)</param>
    /// <param name="lon">Longitude in decimal degrees (-180 to 180)</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of G-AIRMETs affecting the given point</returns>
    /// <response code="200">Returns the G-AIRMETs found</response>
    /// <response code="400">If coordinates are invalid</response>
    [HttpGet("affecting")]
    [ProducesResponseType(typeof(PaginatedResponse<GAirmetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<GAirmetDto>>> SearchAffecting(
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
        return Ok(await gairmetService.SearchAffecting(lat, lon, pagination.Cursor, pagination.Limit, ct));
    }
}

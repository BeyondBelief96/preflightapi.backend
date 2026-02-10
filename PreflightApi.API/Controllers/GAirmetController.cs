using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Domain.Enums;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

/// <summary>
/// Provides access to G-AIRMETs (Graphical AIRMETs) — graphical weather advisories issued by the Aviation Weather Center.
/// G-AIRMETs are organized by product type: SIERRA (IFR/mountain obscuration), TANGO (turbulence/wind shear/surface winds),
/// and ZULU (icing/freezing level). They can also be queried by specific hazard type (e.g., ICE, TURB_LO, IFR).
/// Each advisory includes the hazard, severity, altitude range, and geographic polygon of the affected area.
/// G-AIRMETs are issued every 3 hours with forecasts at 0, 3, 6, 9, and 12 hour intervals.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/g-airmets")]
[Tags("Weather - G-AIRMETs")]
public class GAirmetController(IGAirmetService gairmetService) : ControllerBase
{
    /// <summary>
    /// Gets all current G-AIRMETs (Graphical AIRMETs)
    /// </summary>
    /// <returns>All active G-AIRMET advisories</returns>
    /// <response code="200">Returns the list of G-AIRMETs</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<GAirmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GAirmetDto>>> GetAllGAirmets()
    {
        return Ok(await gairmetService.GetAllGAirmets());
    }

    /// <summary>
    /// Gets G-AIRMETs filtered by product type
    /// </summary>
    /// <param name="product">Product type: SIERRA, TANGO, or ZULU</param>
    /// <returns>G-AIRMETs matching the specified product type</returns>
    /// <response code="200">Returns the filtered G-AIRMETs</response>
    /// <response code="400">If the product type is invalid</response>
    [HttpGet("{product}")]
    [ProducesResponseType(typeof(List<GAirmetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<GAirmetDto>>> GetGAirmetsByProduct(string product)
    {
        if (!Enum.TryParse<GAirmetProduct>(product, ignoreCase: true, out var productEnum))
            throw new ValidationException("product", $"Invalid product type '{product}'. Valid values are: SIERRA, TANGO, ZULU");

        return Ok(await gairmetService.GetGAirmetsByProduct(productEnum));
    }

    /// <summary>
    /// Gets Sierra G-AIRMETs (IFR and mountain obscuration)
    /// </summary>
    /// <returns>Sierra product G-AIRMETs</returns>
    /// <response code="200">Returns Sierra G-AIRMETs</response>
    [HttpGet("sierra")]
    [ProducesResponseType(typeof(List<GAirmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GAirmetDto>>> GetSierraGAirmets()
    {
        return Ok(await gairmetService.GetGAirmetsByProduct(GAirmetProduct.SIERRA));
    }

    /// <summary>
    /// Gets Tango G-AIRMETs (turbulence, low-level wind shear, and strong surface winds)
    /// </summary>
    /// <returns>Tango product G-AIRMETs</returns>
    /// <response code="200">Returns Tango G-AIRMETs</response>
    [HttpGet("tango")]
    [ProducesResponseType(typeof(List<GAirmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GAirmetDto>>> GetTangoGAirmets()
    {
        return Ok(await gairmetService.GetGAirmetsByProduct(GAirmetProduct.TANGO));
    }

    /// <summary>
    /// Gets Zulu G-AIRMETs (icing and freezing level)
    /// </summary>
    /// <returns>Zulu product G-AIRMETs</returns>
    /// <response code="200">Returns Zulu G-AIRMETs</response>
    [HttpGet("zulu")]
    [ProducesResponseType(typeof(List<GAirmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GAirmetDto>>> GetZuluGAirmets()
    {
        return Ok(await gairmetService.GetGAirmetsByProduct(GAirmetProduct.ZULU));
    }

    // ==================== Hazard Type Endpoints ====================

    /// <summary>
    /// Gets G-AIRMETs filtered by hazard type
    /// </summary>
    /// <param name="hazardType">Hazard type: MT_OBSC, IFR, TURB_LO, TURB_HI, LLWS, SFC_WIND, ICE, FZLVL, or M_FZLVL</param>
    /// <returns>G-AIRMETs matching the specified hazard type</returns>
    /// <response code="200">Returns the filtered G-AIRMETs</response>
    /// <response code="400">If the hazard type is invalid</response>
    [HttpGet("hazard/{hazardType}")]
    [ProducesResponseType(typeof(List<GAirmetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<GAirmetDto>>> GetGAirmetsByHazardType(string hazardType)
    {
        if (!Enum.TryParse<GAirmetHazardType>(hazardType, ignoreCase: true, out var hazardTypeEnum))
            throw new ValidationException("hazardType", $"Invalid hazard type '{hazardType}'. Valid values are: MT_OBSC, IFR, TURB_LO, TURB_HI, LLWS, SFC_WIND, ICE, FZLVL, M_FZLVL");

        return Ok(await gairmetService.GetGAirmetsByHazardType(hazardTypeEnum));
    }

    // SIERRA Hazard Types

    /// <summary>
    /// Gets mountain obscuration G-AIRMETs
    /// </summary>
    /// <returns>Mountain obscuration G-AIRMETs</returns>
    /// <response code="200">Returns mountain obscuration G-AIRMETs</response>
    [HttpGet("hazard/mt-obsc")]
    [ProducesResponseType(typeof(List<GAirmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GAirmetDto>>> GetMtObscGAirmets()
    {
        return Ok(await gairmetService.GetGAirmetsByHazardType(GAirmetHazardType.MT_OBSC));
    }

    /// <summary>
    /// Gets IFR (Instrument Flight Rules) G-AIRMETs
    /// </summary>
    /// <returns>IFR G-AIRMETs</returns>
    /// <response code="200">Returns IFR G-AIRMETs</response>
    [HttpGet("hazard/ifr")]
    [ProducesResponseType(typeof(List<GAirmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GAirmetDto>>> GetIfrGAirmets()
    {
        return Ok(await gairmetService.GetGAirmetsByHazardType(GAirmetHazardType.IFR));
    }

    // TANGO Hazard Types

    /// <summary>
    /// Gets low-level turbulence G-AIRMETs
    /// </summary>
    /// <returns>Low-level turbulence G-AIRMETs</returns>
    /// <response code="200">Returns low-level turbulence G-AIRMETs</response>
    [HttpGet("hazard/turb-lo")]
    [ProducesResponseType(typeof(List<GAirmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GAirmetDto>>> GetTurbLoGAirmets()
    {
        return Ok(await gairmetService.GetGAirmetsByHazardType(GAirmetHazardType.TURB_LO));
    }

    /// <summary>
    /// Gets high-level turbulence G-AIRMETs
    /// </summary>
    /// <returns>High-level turbulence G-AIRMETs</returns>
    /// <response code="200">Returns high-level turbulence G-AIRMETs</response>
    [HttpGet("hazard/turb-hi")]
    [ProducesResponseType(typeof(List<GAirmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GAirmetDto>>> GetTurbHiGAirmets()
    {
        return Ok(await gairmetService.GetGAirmetsByHazardType(GAirmetHazardType.TURB_HI));
    }

    /// <summary>
    /// Gets low-level wind shear G-AIRMETs
    /// </summary>
    /// <returns>Low-level wind shear G-AIRMETs</returns>
    /// <response code="200">Returns low-level wind shear G-AIRMETs</response>
    [HttpGet("hazard/llws")]
    [ProducesResponseType(typeof(List<GAirmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GAirmetDto>>> GetLlwsGAirmets()
    {
        return Ok(await gairmetService.GetGAirmetsByHazardType(GAirmetHazardType.LLWS));
    }

    /// <summary>
    /// Gets strong surface wind G-AIRMETs
    /// </summary>
    /// <returns>Strong surface wind G-AIRMETs</returns>
    /// <response code="200">Returns strong surface wind G-AIRMETs</response>
    [HttpGet("hazard/sfc-wind")]
    [ProducesResponseType(typeof(List<GAirmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GAirmetDto>>> GetSfcWindGAirmets()
    {
        return Ok(await gairmetService.GetGAirmetsByHazardType(GAirmetHazardType.SFC_WIND));
    }

    // ZULU Hazard Types

    /// <summary>
    /// Gets icing G-AIRMETs
    /// </summary>
    /// <returns>Icing G-AIRMETs</returns>
    /// <response code="200">Returns icing G-AIRMETs</response>
    [HttpGet("hazard/ice")]
    [ProducesResponseType(typeof(List<GAirmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GAirmetDto>>> GetIceGAirmets()
    {
        return Ok(await gairmetService.GetGAirmetsByHazardType(GAirmetHazardType.ICE));
    }

    /// <summary>
    /// Gets freezing level G-AIRMETs
    /// </summary>
    /// <returns>Freezing level G-AIRMETs</returns>
    /// <response code="200">Returns freezing level G-AIRMETs</response>
    [HttpGet("hazard/fzlvl")]
    [ProducesResponseType(typeof(List<GAirmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GAirmetDto>>> GetFzlvlGAirmets()
    {
        return Ok(await gairmetService.GetGAirmetsByHazardType(GAirmetHazardType.FZLVL));
    }

    /// <summary>
    /// Gets multiple freezing level G-AIRMETs
    /// </summary>
    /// <returns>Multiple freezing level G-AIRMETs</returns>
    /// <response code="200">Returns multiple freezing level G-AIRMETs</response>
    [HttpGet("hazard/m-fzlvl")]
    [ProducesResponseType(typeof(List<GAirmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GAirmetDto>>> GetMFzlvlGAirmets()
    {
        return Ok(await gairmetService.GetGAirmetsByHazardType(GAirmetHazardType.M_FZLVL));
    }
}

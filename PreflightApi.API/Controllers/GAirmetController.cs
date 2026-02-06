using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Domain.Enums;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/g-airmets")]
public class GAirmetController(IGAirmetService gairmetService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<GAirmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GAirmetDto>>> GetAllGAirmets()
    {
        return Ok(await gairmetService.GetAllGAirmets());
    }

    [HttpGet("{product}")]
    [ProducesResponseType(typeof(List<GAirmetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<GAirmetDto>>> GetGAirmetsByProduct(string product)
    {
        if (!Enum.TryParse<GAirmetProduct>(product, ignoreCase: true, out var productEnum))
            throw new ValidationException("product", $"Invalid product type '{product}'. Valid values are: SIERRA, TANGO, ZULU");

        return Ok(await gairmetService.GetGAirmetsByProduct(productEnum));
    }

    [HttpGet("sierra")]
    [ProducesResponseType(typeof(List<GAirmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GAirmetDto>>> GetSierraGAirmets()
    {
        return Ok(await gairmetService.GetGAirmetsByProduct(GAirmetProduct.SIERRA));
    }

    [HttpGet("tango")]
    [ProducesResponseType(typeof(List<GAirmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GAirmetDto>>> GetTangoGAirmets()
    {
        return Ok(await gairmetService.GetGAirmetsByProduct(GAirmetProduct.TANGO));
    }

    [HttpGet("zulu")]
    [ProducesResponseType(typeof(List<GAirmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GAirmetDto>>> GetZuluGAirmets()
    {
        return Ok(await gairmetService.GetGAirmetsByProduct(GAirmetProduct.ZULU));
    }

    // ==================== Hazard Type Endpoints ====================

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

    [HttpGet("hazard/mt-obsc")]
    [ProducesResponseType(typeof(List<GAirmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GAirmetDto>>> GetMtObscGAirmets()
    {
        return Ok(await gairmetService.GetGAirmetsByHazardType(GAirmetHazardType.MT_OBSC));
    }

    [HttpGet("hazard/ifr")]
    [ProducesResponseType(typeof(List<GAirmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GAirmetDto>>> GetIfrGAirmets()
    {
        return Ok(await gairmetService.GetGAirmetsByHazardType(GAirmetHazardType.IFR));
    }

    // TANGO Hazard Types

    [HttpGet("hazard/turb-lo")]
    [ProducesResponseType(typeof(List<GAirmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GAirmetDto>>> GetTurbLoGAirmets()
    {
        return Ok(await gairmetService.GetGAirmetsByHazardType(GAirmetHazardType.TURB_LO));
    }

    [HttpGet("hazard/turb-hi")]
    [ProducesResponseType(typeof(List<GAirmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GAirmetDto>>> GetTurbHiGAirmets()
    {
        return Ok(await gairmetService.GetGAirmetsByHazardType(GAirmetHazardType.TURB_HI));
    }

    [HttpGet("hazard/llws")]
    [ProducesResponseType(typeof(List<GAirmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GAirmetDto>>> GetLlwsGAirmets()
    {
        return Ok(await gairmetService.GetGAirmetsByHazardType(GAirmetHazardType.LLWS));
    }

    [HttpGet("hazard/sfc-wind")]
    [ProducesResponseType(typeof(List<GAirmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GAirmetDto>>> GetSfcWindGAirmets()
    {
        return Ok(await gairmetService.GetGAirmetsByHazardType(GAirmetHazardType.SFC_WIND));
    }

    // ZULU Hazard Types

    [HttpGet("hazard/ice")]
    [ProducesResponseType(typeof(List<GAirmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GAirmetDto>>> GetIceGAirmets()
    {
        return Ok(await gairmetService.GetGAirmetsByHazardType(GAirmetHazardType.ICE));
    }

    [HttpGet("hazard/fzlvl")]
    [ProducesResponseType(typeof(List<GAirmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GAirmetDto>>> GetFzlvlGAirmets()
    {
        return Ok(await gairmetService.GetGAirmetsByHazardType(GAirmetHazardType.FZLVL));
    }

    [HttpGet("hazard/m-fzlvl")]
    [ProducesResponseType(typeof(List<GAirmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GAirmetDto>>> GetMFzlvlGAirmets()
    {
        return Ok(await gairmetService.GetGAirmetsByHazardType(GAirmetHazardType.M_FZLVL));
    }
}

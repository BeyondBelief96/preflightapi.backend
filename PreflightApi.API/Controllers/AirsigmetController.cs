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
[Route("api/v{version:apiVersion}/airsigmets")]
public class AirsigmetController(IAirsigmetService airsigmetService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<AirsigmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AirsigmetDto>>> GetAllAirsigmets()
    {
        return Ok(await airsigmetService.GetAllAirsigmets());
    }

    [HttpGet("hazard/{hazardType}")]
    [ProducesResponseType(typeof(List<AirsigmetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<AirsigmetDto>>> GetAirsigmetsByHazardType(string hazardType)
    {
        if (!Enum.TryParse<AirsigmetHazardType>(hazardType, ignoreCase: true, out var hazardTypeEnum))
            throw new ValidationException("hazardType", $"Invalid hazard type '{hazardType}'. Valid values are: CONVECTIVE, ICE, TURB, IFR, MTN_OBSCN");

        return Ok(await airsigmetService.GetAirsigmetsByHazardType(hazardTypeEnum));
    }

    [HttpGet("convective")]
    [ProducesResponseType(typeof(List<AirsigmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AirsigmetDto>>> GetConvectiveAirsigmets()
    {
        return Ok(await airsigmetService.GetAirsigmetsByHazardType(AirsigmetHazardType.CONVECTIVE));
    }

    [HttpGet("ice")]
    [ProducesResponseType(typeof(List<AirsigmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AirsigmetDto>>> GetIceAirsigmets()
    {
        return Ok(await airsigmetService.GetAirsigmetsByHazardType(AirsigmetHazardType.ICE));
    }

    [HttpGet("turb")]
    [ProducesResponseType(typeof(List<AirsigmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AirsigmetDto>>> GetTurbAirsigmets()
    {
        return Ok(await airsigmetService.GetAirsigmetsByHazardType(AirsigmetHazardType.TURB));
    }

    [HttpGet("ifr")]
    [ProducesResponseType(typeof(List<AirsigmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AirsigmetDto>>> GetIfrAirsigmets()
    {
        return Ok(await airsigmetService.GetAirsigmetsByHazardType(AirsigmetHazardType.IFR));
    }

    [HttpGet("mtn-obscn")]
    [ProducesResponseType(typeof(List<AirsigmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AirsigmetDto>>> GetMtnObscnAirsigmets()
    {
        return Ok(await airsigmetService.GetAirsigmetsByHazardType(AirsigmetHazardType.MTN_OBSCN));
    }
}

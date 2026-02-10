using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Domain.Enums;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

/// <summary>
/// Provides access to AIRMET and SIGMET advisories. AIRMETs warn of weather hazards significant to light aircraft
/// (moderate turbulence/icing, IFR conditions, mountain obscuration). SIGMETs warn of severe weather hazards
/// significant to all aircraft (severe turbulence/icing, convective activity). Each advisory includes the
/// hazard type, severity, altitude range, and geographic polygon defining the affected area.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/airsigmets")]
[Tags("Weather - AIRMETs/SIGMETs")]
public class AirsigmetController(IAirsigmetService airsigmetService) : ControllerBase
{
    /// <summary>
    /// Gets all current AIRMETs and SIGMETs
    /// </summary>
    /// <returns>All active AIRMET and SIGMET advisories</returns>
    /// <response code="200">Returns the list of AIRMETs/SIGMETs</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<AirsigmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AirsigmetDto>>> GetAllAirsigmets()
    {
        return Ok(await airsigmetService.GetAllAirsigmets());
    }

    /// <summary>
    /// Gets AIRMETs/SIGMETs filtered by hazard type
    /// </summary>
    /// <param name="hazardType">Hazard type: CONVECTIVE, ICE, TURB, IFR, or MTN_OBSCN</param>
    /// <returns>AIRMETs/SIGMETs matching the specified hazard type</returns>
    /// <response code="200">Returns the filtered AIRMETs/SIGMETs</response>
    /// <response code="400">If the hazard type is invalid</response>
    [HttpGet("hazard/{hazardType}")]
    [ProducesResponseType(typeof(List<AirsigmetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<AirsigmetDto>>> GetAirsigmetsByHazardType(string hazardType)
    {
        if (!Enum.TryParse<AirsigmetHazardType>(hazardType, ignoreCase: true, out var hazardTypeEnum))
            throw new ValidationException("hazardType", $"Invalid hazard type '{hazardType}'. Valid values are: CONVECTIVE, ICE, TURB, IFR, MTN_OBSCN");

        return Ok(await airsigmetService.GetAirsigmetsByHazardType(hazardTypeEnum));
    }

    /// <summary>
    /// Gets all convective AIRMETs/SIGMETs (thunderstorms)
    /// </summary>
    /// <returns>Convective AIRMETs/SIGMETs</returns>
    /// <response code="200">Returns convective AIRMETs/SIGMETs</response>
    [HttpGet("convective")]
    [ProducesResponseType(typeof(List<AirsigmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AirsigmetDto>>> GetConvectiveAirsigmets()
    {
        return Ok(await airsigmetService.GetAirsigmetsByHazardType(AirsigmetHazardType.CONVECTIVE));
    }

    /// <summary>
    /// Gets all icing AIRMETs/SIGMETs
    /// </summary>
    /// <returns>Icing AIRMETs/SIGMETs</returns>
    /// <response code="200">Returns icing AIRMETs/SIGMETs</response>
    [HttpGet("ice")]
    [ProducesResponseType(typeof(List<AirsigmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AirsigmetDto>>> GetIceAirsigmets()
    {
        return Ok(await airsigmetService.GetAirsigmetsByHazardType(AirsigmetHazardType.ICE));
    }

    /// <summary>
    /// Gets all turbulence AIRMETs/SIGMETs
    /// </summary>
    /// <returns>Turbulence AIRMETs/SIGMETs</returns>
    /// <response code="200">Returns turbulence AIRMETs/SIGMETs</response>
    [HttpGet("turb")]
    [ProducesResponseType(typeof(List<AirsigmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AirsigmetDto>>> GetTurbAirsigmets()
    {
        return Ok(await airsigmetService.GetAirsigmetsByHazardType(AirsigmetHazardType.TURB));
    }

    /// <summary>
    /// Gets all IFR (Instrument Flight Rules) AIRMETs/SIGMETs
    /// </summary>
    /// <returns>IFR AIRMETs/SIGMETs</returns>
    /// <response code="200">Returns IFR AIRMETs/SIGMETs</response>
    [HttpGet("ifr")]
    [ProducesResponseType(typeof(List<AirsigmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AirsigmetDto>>> GetIfrAirsigmets()
    {
        return Ok(await airsigmetService.GetAirsigmetsByHazardType(AirsigmetHazardType.IFR));
    }

    /// <summary>
    /// Gets all mountain obscuration AIRMETs/SIGMETs
    /// </summary>
    /// <returns>Mountain obscuration AIRMETs/SIGMETs</returns>
    /// <response code="200">Returns mountain obscuration AIRMETs/SIGMETs</response>
    [HttpGet("mtn-obscn")]
    [ProducesResponseType(typeof(List<AirsigmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AirsigmetDto>>> GetMtnObscnAirsigmets()
    {
        return Ok(await airsigmetService.GetAirsigmetsByHazardType(AirsigmetHazardType.MTN_OBSCN));
    }
}

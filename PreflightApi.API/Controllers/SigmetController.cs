using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Domain.Enums;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos;
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
    /// <returns>All active domestic SIGMET advisories with hazard details and geographic boundaries</returns>
    /// <response code="200">Returns the list of all current domestic SIGMETs</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<SigmetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SigmetDto>>> GetAllSigmets()
    {
        return Ok(await sigmetService.GetAllSigmets());
    }

    /// <summary>
    /// Gets SIGMETs filtered by hazard type
    /// </summary>
    /// <param name="hazardType">Hazard type: CONVECTIVE, ICE, TURB, IFR, or MTN_OBSCN</param>
    /// <returns>SIGMETs matching the specified hazard type</returns>
    /// <response code="200">Returns the filtered SIGMETs</response>
    /// <response code="400">If the hazard type is invalid</response>
    [HttpGet("hazard/{hazardType}")]
    [ProducesResponseType(typeof(List<SigmetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<SigmetDto>>> GetSigmetsByHazardType(string hazardType)
    {
        if (!Enum.TryParse<SigmetHazardType>(hazardType, ignoreCase: true, out var hazardTypeEnum))
            throw new ValidationException("hazardType", $"Invalid hazard type '{hazardType}'. Valid values are: CONVECTIVE, ICE, TURB, IFR, MTN_OBSCN");

        return Ok(await sigmetService.GetSigmetsByHazardType(hazardTypeEnum));
    }
}

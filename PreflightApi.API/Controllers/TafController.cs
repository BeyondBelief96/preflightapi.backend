using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

/// <summary>
/// Provides access to decoded TAF (Terminal Aerodrome Forecast) products.
/// TAFs are weather forecasts for airports, typically covering a 24-30 hour period with forecast periods
/// describing expected wind, visibility, sky conditions, and weather phenomena. TAFs are issued
/// approximately every 6 hours for airports with weather reporting capabilities.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/tafs")]
[Tags("Weather - TAFs")]
public class TafController(ITafService tafService) : ControllerBase
{
    /// <summary>
    /// Gets the current TAFs for multiple airports in a single request.
    /// Accepts ICAO codes or FAA identifiers. Identifiers that don't resolve to a TAF are silently skipped.
    /// </summary>
    /// <remarks>
    /// Both ICAO codes and FAA identifiers can be mixed in the same request.
    /// Maximum 100 identifiers per request.
    /// <code>
    /// GET /api/v1/tafs/batch?ids=KDFW,KAUS,KHOU
    /// GET /api/v1/tafs/batch?ids=DFW,AUS
    /// </code>
    /// </remarks>
    /// <param name="ids">Comma-separated ICAO codes or FAA identifiers (e.g., <c>KDFW,KAUS,KHOU</c>). Maximum 100.</param>
    /// <returns>TAFs for the requested airports</returns>
    /// <response code="200">Returns the TAFs with all forecast periods</response>
    /// <response code="400">If the ids parameter is empty or exceeds 100 identifiers</response>
    [HttpGet("batch")]
    [ProducesResponseType(typeof(IEnumerable<TafDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<TafDto>>> GetTafsBatch(
        [FromQuery] string ids)
    {
        if (string.IsNullOrWhiteSpace(ids))
            throw new ValidationException("ids", "At least one ICAO code or identifier is required");

        var codesArray = ids.Split(',')
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToArray();

        var tafs = await tafService.GetTafsForAirports(codesArray);
        return Ok(tafs);
    }

    /// <summary>
    /// Gets the current TAF for a specific airport, including all forecast periods with expected
    /// weather conditions (wind, visibility, sky cover, precipitation, turbulence, and icing).
    /// </summary>
    /// <remarks>
    /// <code>
    /// GET /api/v1/tafs/KDFW    — by ICAO code
    /// GET /api/v1/tafs/DFW     — by FAA identifier
    /// </code>
    /// </remarks>
    /// <param name="icaoCodeOrIdent">ICAO code or FAA identifier (e.g., KDFW, DFW). Case-insensitive.</param>
    /// <returns>TAF with forecast periods for the specified airport</returns>
    /// <response code="200">Returns the TAF with all forecast periods</response>
    /// <response code="404">If no TAF is found for the airport</response>
    [HttpGet("{icaoCodeOrIdent}")]
    [ProducesResponseType(typeof(TafDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TafDto>> GetTafByIcaoCodeOrIdent(string icaoCodeOrIdent)
    {
        var taf = await tafService.GetTafByIcaoCode(icaoCodeOrIdent);
        return Ok(taf);
    }
}

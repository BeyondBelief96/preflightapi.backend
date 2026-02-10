using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

/// <summary>
/// Provides access to TAF (Terminal Aerodrome Forecast) data.
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
    /// Gets the current TAF for a specific airport, including all forecast periods with expected
    /// weather conditions (wind, visibility, sky cover, precipitation, turbulence, and icing).
    /// </summary>
    /// <param name="icaoCodeOrIdent">ICAO code or FAA identifier (e.g., KDFW, DFW)</param>
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

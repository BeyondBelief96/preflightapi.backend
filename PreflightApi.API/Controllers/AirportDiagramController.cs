using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/airport-diagrams")]
[Tags("Airport Diagrams")]
public class AirportDiagramController(IAirportDiagramService airportDiagramService) : ControllerBase
{
    /// <summary>
    /// Gets pre-signed URLs for all airport diagrams by ICAO code or identifier
    /// </summary>
    /// <param name="icaoCodeOrIdent">ICAO code or airport identifier</param>
    /// <returns>Airport information with pre-signed URLs for all diagram PDFs</returns>
    /// <response code="200">Returns the airport diagrams</response>
    /// <response code="404">If no airport diagrams are found</response>
    [HttpGet("{icaoCodeOrIdent}")]
    [ProducesResponseType(typeof(AirportDiagramsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AirportDiagramsResponseDto>> GetAirportDiagrams(string icaoCodeOrIdent)
    {
        var diagrams = await airportDiagramService.GetAirportDiagramsByAirportCode(icaoCodeOrIdent);
        return Ok(diagrams);
    }
}

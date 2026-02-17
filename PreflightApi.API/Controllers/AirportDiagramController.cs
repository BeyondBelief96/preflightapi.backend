using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

/// <summary>
/// Provides access to FAA airport diagram PDFs. Airport diagrams show taxiways, runways, buildings,
/// and other ground features. Diagrams are returned as time-limited pre-signed URLs for PDF download.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/airport-diagrams")]
[Tags("Airport Diagrams")]
public class AirportDiagramController(IAirportDiagramService airportDiagramService) : ControllerBase
{
    /// <summary>
    /// Gets time-limited pre-signed URLs for all available airport diagram PDFs.
    /// The URLs expire after a limited period; request new URLs if they have expired.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns the airport's ICAO code, name, and a list of diagram URLs.
    /// </para>
    /// <code>
    /// GET /api/v1/airport-diagrams/KDFW
    /// GET /api/v1/airport-diagrams/DFW
    /// </code>
    /// </remarks>
    /// <param name="icaoCodeOrIdent">ICAO code or FAA identifier (e.g., KDFW, DFW). Case-insensitive.</param>
    /// <returns>Airport information with pre-signed URLs for all available diagram PDFs</returns>
    /// <response code="200">Returns the airport diagrams</response>
    /// <response code="404">If no airport diagrams are found for the given identifier</response>
    [HttpGet("{icaoCodeOrIdent}")]
    [ProducesResponseType(typeof(AirportDiagramsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AirportDiagramsResponseDto>> GetAirportDiagrams(string icaoCodeOrIdent)
    {
        var diagrams = await airportDiagramService.GetAirportDiagramsByAirportCode(icaoCodeOrIdent);
        return Ok(diagrams);
    }
}

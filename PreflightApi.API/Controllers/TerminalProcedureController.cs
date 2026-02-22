using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

/// <summary>
/// Provides access to FAA terminal procedure chart PDFs from the Digital Terminal Procedures Publication (d-TPP).
/// Includes Instrument Approach Procedures (IAP), Departure Procedures (DP), Standard Terminal Arrivals (STAR),
/// Airport Diagrams (APD), Minimums (MIN), Hot Spots (HOT), and more.
/// Charts are returned as time-limited pre-signed URLs for PDF download.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/terminal-procedures")]
[Tags("Terminal Procedures")]
public class TerminalProcedureController(ITerminalProcedureService terminalProcedureService) : ControllerBase
{
    /// <summary>
    /// Gets time-limited pre-signed URLs for all available terminal procedure chart PDFs.
    /// The URLs expire after a limited period; request new URLs if they have expired.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns the airport's ICAO code, name, and a list of procedure chart URLs.
    /// Optionally filter by chart code (IAP, DP, STAR, APD, MIN, HOT, etc.).
    /// </para>
    /// <code>
    /// GET /api/v1/terminal-procedures/KDFW
    /// GET /api/v1/terminal-procedures/DFW?chartCode=IAP
    /// </code>
    /// </remarks>
    /// <param name="icaoCodeOrIdent">ICAO code or FAA identifier (e.g., KDFW, DFW). Case-insensitive.</param>
    /// <param name="chartCode">Optional chart code filter (e.g., IAP, DP, STAR, APD, MIN, HOT). Case-insensitive.</param>
    /// <returns>Airport information with pre-signed URLs for available terminal procedure chart PDFs</returns>
    /// <response code="200">Returns the terminal procedures</response>
    /// <response code="404">If no terminal procedures are found for the given identifier</response>
    [HttpGet("{icaoCodeOrIdent}")]
    [ProducesResponseType(typeof(TerminalProceduresResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TerminalProceduresResponseDto>> GetTerminalProcedures(
        string icaoCodeOrIdent,
        [FromQuery] string? chartCode = null)
    {
        var procedures = await terminalProcedureService.GetTerminalProceduresByAirportCode(icaoCodeOrIdent, chartCode);
        return Ok(procedures);
    }
}

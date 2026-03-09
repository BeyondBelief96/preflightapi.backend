using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.API.Utilities;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

/// <summary>
/// Provides access to airport and facility communication frequency data from the FAA NASR database.
/// Includes tower, ground, ATIS, approach/departure, clearance delivery, and other radio frequencies
/// associated with airports and air traffic control facilities.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/communication-frequencies")]
[Tags("Communication Frequencies")]
public class CommunicationFrequencyController(ICommunicationFrequencyService frequencyService)
    : ControllerBase
{
    /// <summary>
    /// Gets all communication frequencies for a serviced facility (airport or ATC facility).
    /// Returns frequencies including their intended use (e.g., TWR, GND, ATIS, APP, DEP),
    /// call signs, operating hours, and sectorization details.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Frequencies are stored under the FAA airport code (e.g., <c>DFW</c>), but you can pass either
    /// format — ICAO prefixes (<c>K</c>, <c>P</c>, <c>H</c>) are automatically stripped
    /// (e.g., <c>KDFW</c> resolves to <c>DFW</c>, <c>PA88</c> resolves to <c>A88</c>).
    /// </para>
    /// <code>
    /// GET /api/v1/communication-frequencies/DFW      — by FAA identifier
    /// GET /api/v1/communication-frequencies/KDFW     — ICAO prefix stripped automatically
    /// GET /api/v1/communication-frequencies/KW05     — resolves to FAA identifier W05
    /// </code>
    /// </remarks>
    /// <param name="servicedFacility">ICAO code or FAA identifier (e.g., KDFW, DFW). Case-insensitive. ICAO prefixes are automatically stripped to resolve the FAA facility code.</param>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <returns>Paginated list of communication frequencies for the facility</returns>
    /// <response code="200">Returns the communication frequencies</response>
    /// <response code="400">If the serviced facility identifier is empty</response>
    /// <response code="404">If the facility is not found</response>
    [HttpGet("{servicedFacility}")]
    [ProducesResponseType(typeof(PaginatedResponse<CommunicationFrequencyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaginatedResponse<CommunicationFrequencyDto>>> GetFrequenciesByServicedFacility(
        string servicedFacility,
        [FromQuery] PaginationParams pagination,
        CancellationToken ct)
    {
        ValidationHelpers.ValidateRequiredString(servicedFacility, "servicedFacility", "Serviced facility identifier is required");

        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await frequencyService.GetFrequenciesByServicedFacility(servicedFacility, pagination.Cursor, pagination.Limit, ct));
    }
}

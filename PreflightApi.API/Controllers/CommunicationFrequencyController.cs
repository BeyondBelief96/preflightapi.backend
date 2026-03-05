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
    /// The facility identifier is the FAA airport code — typically the ICAO code without the leading
    /// "K" prefix (e.g., <c>DFW</c> not <c>KDFW</c>). This corresponds to the <c>ArptId</c> field
    /// returned by the Airports endpoint.
    /// </para>
    /// <code>
    /// GET /api/v1/communication-frequencies/DFW
    /// GET /api/v1/communication-frequencies/AUS
    /// </code>
    /// </remarks>
    /// <param name="servicedFacility">FAA facility identifier — the FAA airport code without the "K" prefix (e.g., <c>DFW</c>, <c>AUS</c>). Use the <c>ArptId</c> field from the Airports endpoint.</param>
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

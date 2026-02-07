using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/communication-frequencies")]
[Tags("Communication Frequencies")]
public class CommunicationFrequencyController(ICommunicationFrequencyService frequencyService)
    : ControllerBase
{
    /// <summary>
    /// Gets communication frequencies for a serviced facility
    /// </summary>
    /// <param name="servicedFacility">FAA facility identifier (e.g., DFW, AUS)</param>
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
        [FromQuery] PaginationParams pagination)
    {
        if (string.IsNullOrWhiteSpace(servicedFacility))
            throw new ValidationException("servicedFacility", "Serviced facility identifier is required");

        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await frequencyService.GetFrequenciesByServicedFacility(servicedFacility, pagination.Cursor, pagination.Limit));
    }
}

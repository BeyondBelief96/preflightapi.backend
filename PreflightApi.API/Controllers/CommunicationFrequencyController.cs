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
public class CommunicationFrequencyController(ICommunicationFrequencyService frequencyService)
    : ControllerBase
{
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

using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Authentication;
using PreflightApi.API.Models;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

[ApiController]
[Route("api/communication-frequencies")]
[ConditionalAuth]
public class CommunicationFrequencyController(ICommunicationFrequencyService frequencyService)
    : ControllerBase
{
    /// <summary>
    /// Gets all communication frequencies for a specific serviced facility
    /// </summary>
    /// <param name="servicedFacility">The serviced facility identifier</param>
    /// <returns>List of communication frequencies for the facility</returns>
    /// <response code="200">Returns the list of communication frequencies</response>
    /// <response code="400">If the serviced facility identifier is empty</response>
    /// <response code="404">If the airport/facility is not found</response>
    [HttpGet("{servicedFacility}")]
    [ProducesResponseType(typeof(IEnumerable<CommunicationFrequencyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<CommunicationFrequencyDto>>> GetFrequenciesByServicedFacility(
        string servicedFacility)
    {
        if (string.IsNullOrWhiteSpace(servicedFacility))
        {
            throw new ValidationException("servicedFacility", "Serviced facility identifier is required");
        }

        var frequencies = await frequencyService.GetFrequenciesByServicedFacility(servicedFacility);
        return Ok(frequencies);
    }
}

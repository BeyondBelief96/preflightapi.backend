using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Authentication;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    [HttpGet("{servicedFacility}")]
    [ProducesResponseType(typeof(IEnumerable<CommunicationFrequencyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CommunicationFrequencyDto>>> GetFrequenciesByServicedFacility(
        string servicedFacility)
    {
        var frequencies = await frequencyService.GetFrequenciesByServicedFacility(servicedFacility);
        return Ok(frequencies);
    }
}

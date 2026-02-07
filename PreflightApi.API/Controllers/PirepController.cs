using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/pireps")]
[Tags("Weather - PIREPs")]
public class PirepController(IPirepService pirepService) : ControllerBase
{
    /// <summary>
    /// Gets all current PIREPs (Pilot Reports)
    /// </summary>
    /// <returns>All active pilot reports including turbulence, icing, and sky conditions</returns>
    /// <response code="200">Returns the list of PIREPs</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<PirepDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PirepDto>>> GetAllPireps()
    {
        return Ok(await pirepService.GetAllPireps());
    }
}

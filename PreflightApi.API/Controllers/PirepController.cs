using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/pireps")]
public class PirepController(IPirepService pirepService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<PirepDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PirepDto>>> GetAllPireps()
    {
        return Ok(await pirepService.GetAllPireps());
    }
}

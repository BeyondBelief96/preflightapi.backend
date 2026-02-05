using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Authentication;
using PreflightApi.API.Models;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ConditionalAuth]
    public class PirepController(IPirepService pirepService)
        : ControllerBase
    {
        /// <summary>
        /// Gets all PIREPs
        /// </summary>
        /// <returns>List of all PIREPs</returns>
        /// <response code="200">Returns the list of PIREPs</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<PirepDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<PirepDto>>> GetAllPireps()
        {
            var pireps = await pirepService.GetAllPireps();
            return Ok(pireps);
        }
    }
}

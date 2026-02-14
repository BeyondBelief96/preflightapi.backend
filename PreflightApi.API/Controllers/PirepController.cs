using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

/// <summary>
/// Provides access to PIREPs (Pilot Reports) issued in PIREP or AIREP format — real-time weather observations
/// reported by pilots in flight. PIREPs contain firsthand reports of turbulence, icing, sky conditions,
/// visibility, and other flight conditions at specific altitudes and locations. Unlike METARs (ground-based),
/// PIREPs describe conditions aloft. Report types are UA (routine) or UUA (urgent, indicating severe conditions).
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/pireps")]
[Tags("Weather - PIREPs")]
public class PirepController(IPirepService pirepService) : ControllerBase
{
    /// <summary>
    /// Gets all current PIREPs. Returns all active pilot reports with reported conditions
    /// including turbulence (type, intensity, altitude), icing (type, intensity, altitude),
    /// and sky conditions. Each report includes the geographic coordinates and altitude where
    /// the observation was made.
    /// </summary>
    /// <param name="pagination">Cursor-based pagination parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of active pilot reports including turbulence, icing, and sky conditions</returns>
    /// <response code="200">Returns the paginated list of PIREPs</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<PirepDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<PirepDto>>> GetAllPireps(
        [FromQuery] PaginationParams pagination,
        CancellationToken ct)
    {
        pagination.Limit = Math.Clamp(pagination.Limit, 1, 500);
        return Ok(await pirepService.GetAllPireps(pagination.Cursor, pagination.Limit, ct));
    }
}

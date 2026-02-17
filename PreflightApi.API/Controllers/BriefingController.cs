using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Infrastructure.Dtos.Briefing;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

/// <summary>
/// Provides composite weather briefings for flight routes — a single endpoint that returns
/// METARs, TAFs, PIREPs, SIGMETs, G-AIRMETs, and NOTAMs for all airports and airspace
/// along a planned route of flight. Similar to a standard weather briefing from 1800wxbrief.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/briefing")]
[Tags("Briefing")]
public class BriefingController(IBriefingService briefingService) : ControllerBase
{
    /// <summary>
    /// Generates a composite weather briefing for a flight route. Accepts a list of waypoints
    /// (airport identifiers or lat/lon coordinates) and returns all weather products affecting
    /// the route corridor: METARs and TAFs for airports along the route, PIREPs within the
    /// corridor, SIGMETs and G-AIRMETs intersecting the route, and active NOTAMs.
    /// </summary>
    /// <param name="request">Route definition with waypoints and corridor width</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Composite weather briefing with all products along the route</returns>
    /// <response code="200">Returns the route weather briefing</response>
    /// <response code="400">If the request is invalid (fewer than 2 waypoints, invalid coordinates, etc.)</response>
    /// <response code="404">If an airport waypoint is not found</response>
    [HttpPost("route")]
    [ProducesResponseType(typeof(RouteBriefingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RouteBriefingResponse>> GetRouteBriefing(
        [FromBody] RouteBriefingRequest request,
        CancellationToken ct)
    {
        return Ok(await briefingService.GetRouteBriefingAsync(request, ct));
    }
}

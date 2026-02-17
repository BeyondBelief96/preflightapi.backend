using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Infrastructure.Dtos.Briefing;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

/// <summary>
/// Provides composite weather briefings for flight routes — a single endpoint that returns
/// METARs, TAFs, PIREPs, SIGMETs, G-AIRMETs, and NOTAMs for all airports and airspace
/// along a planned route of flight.
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
    /// <remarks>
    /// <para>
    /// Each waypoint is either an airport identifier (ICAO or FAA) or a lat/lon coordinate.
    /// At least two waypoints are required. The corridor width controls how far from the route
    /// centerline to search for PIREPs, airports, and NOTAMs (default 25 NM each side).
    /// </para>
    ///
    /// <para><strong>Airport-only route</strong></para>
    /// <code>
    /// {
    ///   "waypoints": [
    ///     { "airportIdentifier": "KDFW" },
    ///     { "airportIdentifier": "KAUS" }
    ///   ]
    /// }
    /// </code>
    ///
    /// <para><strong>Mixed route with coordinate waypoints and custom corridor</strong></para>
    /// <code>
    /// {
    ///   "waypoints": [
    ///     { "airportIdentifier": "KDFW" },
    ///     { "latitude": 31.5, "longitude": -97.2 },
    ///     { "airportIdentifier": "KAUS" }
    ///   ],
    ///   "corridorWidthNm": 30
    /// }
    /// </code>
    ///
    /// <para><strong>Response Contents</strong></para>
    /// <list type="bullet">
    ///   <item><description><c>Metars</c> — latest METARs for airports within the corridor</description></item>
    ///   <item><description><c>Tafs</c> — current TAFs for airports within the corridor</description></item>
    ///   <item><description><c>Pireps</c> — pilot reports within the corridor</description></item>
    ///   <item><description><c>Sigmets</c> — active SIGMETs whose boundaries intersect the route</description></item>
    ///   <item><description><c>GAirmets</c> — active G-AIRMETs whose boundaries intersect the route</description></item>
    ///   <item><description><c>Notams</c> — active NOTAMs for airports along the route</description></item>
    /// </list>
    /// </remarks>
    /// <param name="request">
    /// Route definition containing <c>Waypoints</c> (minimum 2, each with <c>AirportIdentifier</c>
    /// or <c>Latitude</c>/<c>Longitude</c>) and optional <c>CorridorWidthNm</c> (default 25).
    /// </param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Composite weather briefing with METARs, TAFs, PIREPs, SIGMETs, G-AIRMETs, and NOTAMs along the route</returns>
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

using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos.Navlog;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

/// <summary>
/// Provides VFR cross-country flight planning tools including full navigation log calculation,
/// bearing/distance computation, and winds aloft data. The navigation log calculates course, heading,
/// ground speed, time, and fuel for each leg while accounting for wind and magnetic variation.
/// The navlog response also identifies airspaces and obstacles along the route — use the returned
/// IDs with the Airspace and Obstacle endpoints to retrieve full details.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/navlog")]
[Tags("Navigation Log")]
public class NavlogController(INavlogService navlogService)
    : ControllerBase
{
    /// <summary>
    /// Calculates a complete VFR navigation log for a cross-country flight. Provide an ordered list of
    /// waypoints and aircraft performance data (airspeeds, climb/descent rates, fuel burn rates).
    /// The response includes per-leg calculations (true/magnetic course, heading, ground speed, distance,
    /// time, fuel burn, wind data) and identifiers for airspaces and obstacles along the route.
    /// Use the returned AirspaceGlobalIds with <c>GET /api/v1/airspaces/by-global-ids</c>,
    /// SpecialUseAirspaceGlobalIds with <c>GET /api/v1/airspaces/special-use/by-global-ids</c>,
    /// and ObstacleOasNumbers with <c>POST /api/v1/obstacles/by-oas-numbers</c> to get full details.
    /// </summary>
    /// <param name="request">Navigation log request including ordered waypoints, aircraft performance data, cruising altitude, and departure time</param>
    /// <returns>Complete navigation log with per-leg calculations and en-route airspace/obstacle references</returns>
    /// <response code="200">Returns the calculated navigation log</response>
    /// <response code="400">If the request data is invalid (e.g., fewer than 2 waypoints)</response>
    /// <response code="503">If an external service (magnetic variation or winds aloft) is unavailable</response>
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(NavlogResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<NavlogResponseDto>> CalculateNavlog([FromBody] NavlogRequestDto request)
    {
        var response = await navlogService.CalculateNavlog(request);
        return Ok(response);
    }

    /// <summary>
    /// Calculates the great-circle bearing and distance between two geographic points.
    /// Returns true course, magnetic course (adjusted for local magnetic variation), and
    /// distance in nautical miles. Useful for quick point-to-point calculations without
    /// a full navigation log.
    /// </summary>
    /// <param name="request">Start and end point coordinates (latitude/longitude in decimal degrees)</param>
    /// <returns>True course, magnetic course (degrees), and great-circle distance (nautical miles)</returns>
    /// <response code="200">Returns the bearing and distance calculation</response>
    /// <response code="400">If the coordinates are invalid</response>
    /// <response code="503">If the magnetic variation service is unavailable</response>
    [HttpPost("bearing-and-distance")]
    [ProducesResponseType(typeof(BearingAndDistanceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<BearingAndDistanceResponseDto>> CalculateBearingAndDistance(
        [FromBody] BearingAndDistanceRequestDto request)
    {
        var response = await navlogService.CalculateBearingAndDistance(request);
        return Ok(response);
    }

    /// <summary>
    /// Gets winds aloft (FB) forecast data for all reporting sites across the US.
    /// Returns wind direction, speed, and temperature at standard altitude levels (3000, 6000, 9000, 12000,
    /// 18000, 24000, 30000, 34000, 39000 ft MSL) for each reporting site. This is the raw forecast data
    /// that the navlog calculator uses internally to compute wind-corrected headings and ground speeds.
    /// </summary>
    /// <param name="forecast">Forecast period in hours: 6, 12, or 24</param>
    /// <returns>Winds aloft forecast data with wind/temperature at each altitude level for all reporting sites</returns>
    /// <response code="200">Returns the winds aloft data</response>
    /// <response code="400">If the forecast period is not 6, 12, or 24</response>
    /// <response code="503">If the winds aloft data source is unavailable</response>
    [HttpGet("winds-aloft/{forecast}")]
    [ProducesResponseType(typeof(WindsAloftDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<WindsAloftDto>> GetWindsAloftData(int forecast)
    {
        if (forecast != 6 && forecast != 12 && forecast != 24)
            throw new ValidationException("Forecast", "Forecast period must be 6, 12, or 24 hours");

        var response = await navlogService.GetWindsAloftData(forecast);
        return Ok(response);
    }
}

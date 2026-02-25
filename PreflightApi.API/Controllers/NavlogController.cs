using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos.Navlog;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

/// <summary>
/// Provides VFR cross-country flight planning tools.
///
/// <para>
/// This controller offers three capabilities:
/// </para>
///
/// <list type="bullet">
///   <item>
///     <term>Navigation Log</term>
///     <description>
///       Full route calculation with per-leg course, heading, ground speed, time, fuel burn, and wind data.
///       Automatically detects airspaces and obstacles along the route.
///     </description>
///   </item>
///   <item>
///     <term>Bearing and Distance</term>
///     <description>
///       Quick point-to-point great-circle bearing and distance between any two coordinates.
///     </description>
///   </item>
///   <item>
///     <term>Winds Aloft</term>
///     <description>
///       Raw winds aloft (FB) forecast data for all US reporting sites at standard altitude levels.
///     </description>
///   </item>
/// </list>
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/navlog")]
[Tags("Navigation Log")]
public class NavlogController(INavlogService navlogService)
    : ControllerBase
{
    /// <summary>
    /// Calculates a complete VFR navigation log for a cross-country flight.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provide an ordered list of waypoints (minimum 2) with aircraft performance data, a cruising altitude,
    /// and a departure time. The service calculates every leg of the route and returns detailed per-leg data
    /// including course, heading, ground speed, distance, estimated time, fuel burn, and wind information.
    /// </para>
    ///
    /// <para><strong>Automatic Waypoint Insertion</strong></para>
    /// <para>
    /// The service automatically inserts calculated waypoints into the route to model climb and descent phases:
    /// </para>
    ///
    /// <list type="bullet">
    ///   <item>
    ///     <term>Top of Climb (TOC)</term>
    ///     <description>
    ///       Inserted after departure (or after a refueling stop) at the point where the aircraft reaches
    ///       cruising altitude. Position is calculated using the climb true airspeed and climb rate from
    ///       your performance data. Legs before the TOC use climb airspeed and climb fuel burn rate.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>Top of Descent (TOD)</term>
    ///     <description>
    ///       Inserted before the destination (or before a refueling stop) at the point where the aircraft
    ///       should begin descending. Calculated using descent airspeed, descent rate, and a 3 NM final
    ///       approach buffer. Legs after the TOD use descent airspeed and descent fuel burn rate.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>Bottom of Descent (BOD)</term>
    ///     <description>
    ///       Inserted 3 NM before the destination airport at Traffic Pattern Altitude (airport elevation + 1000 ft,
    ///       rounded to the nearest 100 ft). This marks the point where the aircraft levels off at pattern altitude
    ///       for the approach.
    ///     </description>
    ///   </item>
    /// </list>
    ///
    /// <para><strong>Wind Correction</strong></para>
    /// <para>
    /// The service automatically fetches winds aloft forecast data based on your departure time and selects the
    /// nearest reporting station to each waypoint. Wind direction, speed, and temperature are interpolated to
    /// the leg's altitude. The magnetic heading returned for each leg is the actual heading to fly after
    /// accounting for both magnetic variation and wind correction. If wind data is unavailable, the calculation
    /// proceeds with zero-wind assumptions (ground speed equals true airspeed).
    /// </para>
    ///
    /// <para><strong>Fuel Tracking</strong></para>
    /// <para>
    /// Fuel is tracked across the entire route. Start/Taxi/Takeoff (STT) fuel is deducted at departure and again
    /// after each refueling stop. Each leg burns fuel at the rate matching its phase (climb, cruise, or descent).
    /// The <c>RemainingFuelGals</c> field on each leg shows usable fuel remaining at the end of that leg.
    /// Refueling stops can either add a specific number of gallons or refuel to full capacity.
    /// </para>
    ///
    /// <para><strong>Refueling Stops</strong></para>
    /// <para>
    /// Any intermediate waypoint can be marked as a refueling stop. When a refueling stop is present, the route
    /// is segmented so that each segment gets its own independent TOC, TOD, and BOD waypoints. This models
    /// a real multi-leg flight where you climb out after each stop and descend into each landing.
    /// </para>
    ///
    /// <para><strong>Airspace and Obstacle Detection</strong></para>
    /// <para>
    /// After calculating the route, the service queries the database for controlled airspaces (Class B, C, D, E),
    /// special use airspaces (Restricted, Prohibited, MOA, Warning, Alert), and obstacles along the route corridor.
    /// The response includes identifier collections that you can use with other endpoints to retrieve full details:
    /// </para>
    ///
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <c>AirspaceGlobalIds</c> — use with <c>GET /api/v1/airspaces/by-global-ids</c>
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <c>SpecialUseAirspaceGlobalIds</c> — use with <c>GET /api/v1/airspaces/special-use/by-global-ids</c>
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <c>ObstacleOasNumbers</c> — use with <c>POST /api/v1/obstacles/by-oas-numbers</c>
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    /// <param name="request">
    /// Navigation log request containing:
    /// <c>Waypoints</c> (ordered route points with lat/lon/altitude, minimum 2),
    /// <c>PerformanceData</c> (climb/cruise/descent airspeeds in knots, climb/descent rates in FPM,
    /// fuel burn rates in GPH, STT fuel in gallons, and total fuel on board in gallons),
    /// <c>PlannedCruisingAltitude</c> (feet MSL), and
    /// <c>TimeOfDeparture</c> (UTC, used to select the appropriate winds aloft forecast).
    /// </param>
    /// <returns>
    /// Complete navigation log containing: <c>TotalRouteDistance</c> (NM), <c>TotalRouteTimeHours</c>,
    /// <c>TotalFuelUsed</c> (gallons), <c>AverageWindComponent</c> (knots, negative = headwind),
    /// an ordered list of <c>Legs</c> with per-leg calculations, and identifier collections for
    /// en-route airspaces and obstacles.
    /// </returns>
    /// <response code="200">Returns the calculated navigation log</response>
    /// <response code="400">
    /// The request data is invalid. Common causes: fewer than 2 waypoints, missing performance data,
    /// or invalid coordinate values.
    /// </response>
    /// <response code="503">
    /// An external service required for the calculation is temporarily unavailable.
    /// This can be the NOAA magnetic variation API or the winds aloft data source.
    /// </response>
    /// <param name="ct">Cancellation token</param>
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(NavlogResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<NavlogResponseDto>> CalculateNavlog([FromBody] NavlogRequestDto request, CancellationToken ct)
    {
        var response = await navlogService.CalculateNavlog(request, ct);
        return Ok(response);
    }

    /// <summary>
    /// Calculates the great-circle bearing and distance between two geographic points.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provide a start and end point as latitude/longitude in decimal degrees. The service computes:
    /// </para>
    ///
    /// <list type="bullet">
    ///   <item>
    ///     <term>True Course</term>
    ///     <description>
    ///       The initial bearing from start to end referenced to True North (0-360 degrees),
    ///       computed using WGS84 geodesic (great-circle) geometry.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>Magnetic Course</term>
    ///     <description>
    ///       The true course adjusted for local magnetic variation at the start point.
    ///       This is the course you would read on a magnetic compass (0-360 degrees).
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>Distance</term>
    ///     <description>
    ///       The great-circle distance between the two points in nautical miles.
    ///     </description>
    ///   </item>
    /// </list>
    ///
    /// <para>
    /// This endpoint is useful for quick point-to-point calculations without building a full navigation log.
    /// Note that this returns <em>course</em> (direction of the route), not <em>heading</em> (direction the
    /// aircraft nose points). For wind-corrected headings, use the full navigation log endpoint.
    /// </para>
    /// </remarks>
    /// <param name="request">
    /// Start and end point coordinates. All values are in decimal degrees
    /// (e.g., 36.1245 for latitude, -86.6782 for longitude).
    /// </param>
    /// <returns>
    /// <c>TrueCourse</c> (degrees), <c>MagneticCourse</c> (degrees), and <c>Distance</c> (nautical miles).
    /// </returns>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Returns the bearing and distance calculation</response>
    /// <response code="400">The coordinates are invalid (e.g., latitude outside -90 to 90 range)</response>
    /// <response code="503">The NOAA magnetic variation service is temporarily unavailable</response>
    [HttpPost("bearing-and-distance")]
    [ProducesResponseType(typeof(BearingAndDistanceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<BearingAndDistanceResponseDto>> CalculateBearingAndDistance(
        [FromBody] BearingAndDistanceRequestDto request, CancellationToken ct)
    {
        var response = await navlogService.CalculateBearingAndDistance(request, ct);
        return Ok(response);
    }

    /// <summary>
    /// Retrieves winds aloft (FB) forecast data for all reporting sites across the US.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns wind direction, wind speed, and temperature at standard altitude levels for every
    /// winds aloft reporting station in the United States. This is the same raw forecast data that
    /// the navigation log calculator uses internally to compute wind-corrected headings and ground speeds.
    /// </para>
    ///
    /// <para><strong>Altitude Levels</strong></para>
    /// <para>
    /// Data is provided at the following standard levels (feet MSL):
    /// 3000, 6000, 9000, 12000, 18000, 24000, 30000, 34000, and 39000.
    /// Not all stations report temperature at every level. Wind direction and speed may be null
    /// for calm or light/variable conditions.
    /// </para>
    ///
    /// <para><strong>Forecast Periods</strong></para>
    /// <list type="bullet">
    ///   <item>
    ///     <term>6-hour</term>
    ///     <description>Short-range forecast, most accurate for near-term flights</description>
    ///   </item>
    ///   <item>
    ///     <term>12-hour</term>
    ///     <description>Medium-range forecast for flights departing later in the day</description>
    ///   </item>
    ///   <item>
    ///     <term>24-hour</term>
    ///     <description>Long-range forecast for next-day planning</description>
    ///   </item>
    /// </list>
    ///
    /// <para><strong>Response Structure</strong></para>
    /// <para>
    /// The response includes the forecast validity window (<c>ValidTime</c>, <c>ForUseStartTime</c>,
    /// <c>ForUseEndTime</c>) and a list of reporting sites. Each site has an identifier, coordinates,
    /// and a dictionary of wind/temperature data keyed by altitude level (e.g., "3000", "6000").
    /// Wind direction is in degrees true (the direction wind is blowing <em>from</em>), speed is in knots,
    /// and temperature is in degrees Celsius.
    /// </para>
    /// </remarks>
    /// <param name="forecast">Forecast period in hours. Must be <c>6</c>, <c>12</c>, or <c>24</c>.</param>
    /// <returns>
    /// Winds aloft forecast containing validity times and a list of reporting sites,
    /// each with wind direction (degrees true), speed (knots), and temperature (Celsius)
    /// at standard altitude levels.
    /// </returns>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Returns the winds aloft forecast data</response>
    /// <response code="400">The forecast period is not 6, 12, or 24</response>
    /// <response code="503">The winds aloft data source is temporarily unavailable</response>
    [HttpGet("winds-aloft/{forecast}")]
    [ProducesResponseType(typeof(WindsAloftDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<WindsAloftDto>> GetWindsAloftData(int forecast, CancellationToken ct)
    {
        if (forecast != 6 && forecast != 12 && forecast != 24)
            throw new ValidationException("Forecast", "Forecast period must be 6, 12, or 24 hours");

        var response = await navlogService.GetWindsAloftData(forecast, ct);
        return Ok(response);
    }
}

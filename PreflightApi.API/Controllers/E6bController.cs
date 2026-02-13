using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Infrastructure.Dtos.Performance;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

/// <summary>
/// Provides E6B flight computer calculations for VFR pilots.
///
/// <para>
/// Each calculation is available in two forms:
/// </para>
///
/// <list type="bullet">
///   <item>
///     <term>Airport-based</term>
///     <description>
///       Automatically pulls wind, temperature, and altimeter data from the airport's latest METAR
///       observation. Some airport-based endpoints accept optional overrides for "what if" scenarios.
///     </description>
///   </item>
///   <item>
///     <term>Manual</term>
///     <description>
///       You provide all input values directly. Useful for any location, hypothetical conditions,
///       or when METAR data is not available.
///     </description>
///   </item>
/// </list>
///
/// <para><strong>Available Calculations</strong></para>
/// <list type="bullet">
///   <item>
///     <term>Crosswind</term>
///     <description>Headwind and crosswind components for runway selection</description>
///   </item>
///   <item>
///     <term>Density Altitude</term>
///     <description>Pressure altitude corrected for non-standard temperature</description>
///   </item>
///   <item>
///     <term>Wind Triangle</term>
///     <description>True heading, ground speed, and wind correction angle from course/wind data</description>
///   </item>
///   <item>
///     <term>True Airspeed</term>
///     <description>TAS from calibrated airspeed, pressure altitude, and temperature</description>
///   </item>
/// </list>
/// <list type="bullet">
///   <item>
///     <term>Cloud Base</term>
///     <description>Estimated ceiling height from temperature/dewpoint spread</description>
///   </item>
///   <item>
///     <term>Pressure Altitude</term>
///     <description>Altitude corrected from field elevation and current altimeter setting</description>
///   </item>
/// </list>
///
/// <para>
/// <strong>DISCLAIMER:</strong> These calculations are intended for PRE-FLIGHT planning purposes only
/// and must not be used for in-flight navigation. Results are approximations based on the ICAO Standard
/// Atmosphere (ISA) model, which assumes mid-latitude atmospheric conditions. The actual tropopause
/// altitude varies from ~26,000 ft at the poles to ~55,000 ft at the equator. Always verify against
/// certified instruments and official flight planning tools.
/// </para>
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/e6b")]
[Tags("E6B Flight Computer")]
public class E6bController(IE6bCalculatorService e6bCalculatorService)
    : ControllerBase
{
    /// <summary>
    /// Calculates crosswind and headwind components for every runway at an airport using live METAR wind data.
    ///
    /// <para>
    /// Fetches the airport's latest METAR observation and computes wind components for each runway end.
    /// The response includes a <c>RecommendedRunway</c> — the runway end with the lowest crosswind
    /// that also has a headwind (not a tailwind).
    /// </para>
    ///
    /// <para><strong>Sign Conventions</strong></para>
    /// <list type="bullet">
    ///   <item>
    ///     <description><c>CrosswindKt</c> — positive = wind from the right, negative = wind from the left</description>
    ///   </item>
    ///   <item>
    ///     <description><c>HeadwindKt</c> — positive = headwind (favorable), negative = tailwind (unfavorable)</description>
    ///   </item>
    /// </list>
    ///
    /// <para>
    /// If the METAR reports variable wind (VRB), <c>IsVariableWind</c> is true and crosswind components
    /// are calculated using the full wind speed for all runway ends.
    /// If gusts are reported, separate <c>GustCrosswindKt</c> and <c>GustHeadwindKt</c> fields show the
    /// worst-case gust components.
    /// </para>
    /// </summary>
    /// <param name="icaoCodeOrIdent">ICAO code (e.g., KDFW) or FAA identifier (e.g., DFW)</param>
    /// <returns>
    /// Crosswind data for every runway end at the airport, the METAR wind conditions used,
    /// and the recommended runway identifier.
    /// </returns>
    /// <response code="200">Returns crosswind data for all runways with a recommended runway</response>
    /// <response code="400">The METAR is missing wind direction or wind speed data</response>
    /// <response code="404">The airport was not found, or no current METAR is available for this airport</response>
    [HttpGet("crosswind/{icaoCodeOrIdent}")]
    [ProducesResponseType(typeof(AirportCrosswindResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AirportCrosswindResponseDto>> GetCrosswindForAirport(string icaoCodeOrIdent)
    {
        var result = await e6bCalculatorService.GetCrosswindForAirportAsync(icaoCodeOrIdent);
        return Ok(result);
    }

    /// <summary>
    /// Calculates crosswind and headwind components from manually provided wind and runway heading values.
    ///
    /// <para>
    /// Provide wind direction, wind speed, and a runway heading to compute the headwind and crosswind
    /// components. Optionally include a gust speed to also compute gust components.
    /// </para>
    ///
    /// <para><strong>Sign Conventions</strong></para>
    /// <list type="bullet">
    ///   <item>
    ///     <description><c>CrosswindKt</c> — positive = wind from the right, negative = wind from the left</description>
    ///   </item>
    ///   <item>
    ///     <description><c>HeadwindKt</c> — positive = headwind (favorable), negative = tailwind (unfavorable)</description>
    ///   </item>
    /// </list>
    /// </summary>
    /// <param name="request">
    /// <c>WindDirectionDegrees</c> (0-360, or null for variable),
    /// <c>WindSpeedKt</c> (knots),
    /// <c>WindGustKt</c> (knots, optional),
    /// <c>RunwayHeadingDegrees</c> (magnetic degrees, 0-360).
    /// </param>
    /// <returns>
    /// Crosswind and headwind components in knots, plus gust components if gust speed was provided.
    /// </returns>
    /// <response code="200">Returns the calculated crosswind and headwind components</response>
    /// <response code="400">The request parameters are invalid (e.g., wind speed is negative)</response>
    [HttpPost("crosswind/calculate")]
    [ProducesResponseType(typeof(CrosswindCalculationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<CrosswindCalculationResponseDto> CalculateCrosswind(
        [FromBody] CrosswindCalculationRequestDto request)
    {
        var result = e6bCalculatorService.CalculateCrosswind(request);
        return Ok(result);
    }

    /// <summary>
    /// Calculates density altitude for an airport using live METAR data with optional overrides.
    ///
    /// <para>
    /// Fetches the airport's latest METAR to obtain temperature and altimeter setting, then computes
    /// density altitude using the ISA model. You can optionally override either value via query parameters
    /// for "what if" scenarios (e.g., "what would density altitude be if the temperature reached 40°C?").
    /// </para>
    ///
    /// <para><strong>Response Fields</strong></para>
    /// <list type="bullet">
    ///   <item>
    ///     <description><c>DensityAltitudeFt</c> — the effective altitude the aircraft "feels" based on air density</description>
    ///   </item>
    ///   <item>
    ///     <description><c>PressureAltitudeFt</c> — field elevation corrected for non-standard pressure</description>
    ///   </item>
    ///   <item>
    ///     <description><c>IsaTemperatureCelsius</c> — the standard (ISA) temperature expected at this pressure altitude</description>
    ///   </item>
    ///   <item>
    ///     <description><c>TemperatureDeviationCelsius</c> — how far the actual temperature deviates from ISA (positive = hotter than standard)</description>
    ///   </item>
    /// </list>
    ///
    /// <para>
    /// <em>Formula:</em> <c>DA = PA + 120 * (OAT - ISA_temp)</c>. This does not account for humidity,
    /// local pressure patterns, or non-standard lapse rates.
    /// </para>
    /// </summary>
    /// <param name="icaoCodeOrIdent">ICAO code (e.g., KDFW) or FAA identifier (e.g., DFW)</param>
    /// <param name="request">
    /// Optional query parameters: <c>temperatureCelsiusOverride</c> and/or <c>altimeterInHgOverride</c>.
    /// If omitted, values are taken from the latest METAR.
    /// </param>
    /// <returns>Density altitude, pressure altitude, ISA temperature, temperature deviation, and the METAR data used</returns>
    /// <response code="200">Returns density altitude data for the airport</response>
    /// <response code="400">The METAR is missing temperature or altimeter data and no override was provided</response>
    /// <response code="404">The airport was not found, or no current METAR is available</response>
    [HttpGet("density-altitude/{icaoCodeOrIdent}")]
    [ProducesResponseType(typeof(DensityAltitudeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<DensityAltitudeResponseDto>> GetDensityAltitudeForAirport(
        string icaoCodeOrIdent,
        [FromQuery] AirportDensityAltitudeRequestDto? request = null)
    {
        var result = await e6bCalculatorService.GetDensityAltitudeForAirportAsync(
            icaoCodeOrIdent, request);
        return Ok(result);
    }

    /// <summary>
    /// Calculates density altitude from manually provided field elevation, altimeter setting, and temperature.
    ///
    /// <para>
    /// Provide your own values instead of relying on METAR data. Useful for any location, for planning
    /// with forecast temperatures, or when METAR data is not available.
    /// </para>
    ///
    /// <para><strong>Response Fields</strong></para>
    /// <list type="bullet">
    ///   <item>
    ///     <description><c>DensityAltitudeFt</c> — the effective altitude the aircraft "feels" based on air density</description>
    ///   </item>
    ///   <item>
    ///     <description><c>PressureAltitudeFt</c> — field elevation corrected for non-standard pressure</description>
    ///   </item>
    ///   <item>
    ///     <description><c>IsaTemperatureCelsius</c> — the standard (ISA) temperature expected at this pressure altitude</description>
    ///   </item>
    ///   <item>
    ///     <description><c>TemperatureDeviationCelsius</c> — how far the actual temperature deviates from ISA (positive = hotter than standard)</description>
    ///   </item>
    /// </list>
    ///
    /// <para>
    /// <em>Formula:</em> <c>DA = PA + 120 * (OAT - ISA_temp)</c>. This does not account for humidity,
    /// local pressure patterns, or non-standard lapse rates.
    /// </para>
    /// </summary>
    /// <param name="request">
    /// <c>FieldElevationFt</c> (feet MSL),
    /// <c>AltimeterInHg</c> (inches of mercury),
    /// <c>TemperatureCelsius</c> (degrees Celsius).
    /// </param>
    /// <returns>Density altitude, pressure altitude, ISA temperature, and temperature deviation</returns>
    /// <response code="200">Returns the calculated density altitude</response>
    /// <response code="400">The request parameters are invalid</response>
    [HttpPost("density-altitude/calculate")]
    [ProducesResponseType(typeof(DensityAltitudeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<DensityAltitudeResponseDto> CalculateDensityAltitude(
        [FromBody] DensityAltitudeRequestDto request)
    {
        var result = e6bCalculatorService.CalculateDensityAltitude(request);
        return Ok(result);
    }

    /// <summary>
    /// Solves the wind triangle to compute true heading and ground speed.
    ///
    /// <para>
    /// Given your desired true course, true airspeed, and the wind conditions, this calculates the heading
    /// you need to fly to stay on course and your resulting ground speed.
    /// </para>
    ///
    /// <para><strong>Response Fields</strong></para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <c>TrueHeadingDegrees</c> — the heading to fly (true course + wind correction angle)
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <c>GroundSpeedKt</c> — your speed over the ground after accounting for wind
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <c>WindCorrectionAngleDegrees</c> — the crab angle needed to stay on course
    ///       (positive = correct to the right, negative = correct to the left)
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <c>HeadwindComponentKt</c> — positive = headwind, negative = tailwind
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <c>CrosswindComponentKt</c> — positive = from the right, negative = from the left
    ///     </description>
    ///   </item>
    /// </list>
    ///
    /// <para>
    /// Wind direction is the direction the wind is blowing <em>from</em> (standard meteorological convention).
    /// </para>
    /// </summary>
    /// <param name="request">
    /// <c>TrueCourseDegrees</c> (0-360),
    /// <c>TrueAirspeedKt</c> (knots, must be greater than 0),
    /// <c>WindDirectionDegrees</c> (0-360, direction wind blows from),
    /// <c>WindSpeedKt</c> (knots).
    /// </param>
    /// <returns>True heading, ground speed, wind correction angle, and headwind/crosswind components</returns>
    /// <response code="200">Returns the wind triangle solution</response>
    /// <response code="400">The request parameters are invalid (e.g., TAS is zero or negative)</response>
    [HttpPost("wind-triangle/calculate")]
    [ProducesResponseType(typeof(WindTriangleResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<WindTriangleResponseDto> CalculateWindTriangle(
        [FromBody] WindTriangleRequestDto request)
    {
        var result = e6bCalculatorService.CalculateWindTriangle(request);
        return Ok(result);
    }

    /// <summary>
    /// Calculates true airspeed (TAS) from calibrated airspeed, pressure altitude, and outside air temperature.
    ///
    /// <para>
    /// Uses the full compressible isentropic flow conversion (CAS to impact pressure to Mach to TAS),
    /// accurate from sea level through FL410+ including above the ISA tropopause at 36,089 ft.
    /// This is more accurate than the simplified <c>CAS / sqrt(sigma)</c> formula, which diverges
    /// significantly at higher altitudes.
    /// </para>
    ///
    /// <para><strong>Response Fields</strong></para>
    /// <list type="bullet">
    ///   <item>
    ///     <description><c>TrueAirspeedKt</c> — the aircraft's actual speed through the air mass (knots)</description>
    ///   </item>
    ///   <item>
    ///     <description><c>DensityAltitudeFt</c> — density altitude at the given conditions (feet)</description>
    ///   </item>
    ///   <item>
    ///     <description><c>MachNumber</c> — the aircraft's speed as a fraction of the local speed of sound</description>
    ///   </item>
    /// </list>
    ///
    /// <para>
    /// <em>Note:</em> The ISA tropopause is modeled at 36,089 ft. The real tropopause varies from
    /// ~26,000 ft near the poles to ~55,000 ft near the equator, which affects accuracy at high
    /// altitudes in non-mid-latitude regions.
    /// </para>
    /// </summary>
    /// <param name="request">
    /// <c>CalibratedAirspeedKt</c> (knots, must be greater than 0),
    /// <c>PressureAltitudeFt</c> (feet, can be negative),
    /// <c>OutsideAirTemperatureCelsius</c> (degrees Celsius).
    /// </param>
    /// <returns>True airspeed (knots), density altitude (feet), and Mach number</returns>
    /// <response code="200">Returns the TAS calculation</response>
    /// <response code="400">The request parameters are invalid (e.g., CAS is zero or negative)</response>
    [HttpPost("true-airspeed/calculate")]
    [ProducesResponseType(typeof(TrueAirspeedResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<TrueAirspeedResponseDto> CalculateTrueAirspeed(
        [FromBody] TrueAirspeedRequestDto request)
    {
        var result = e6bCalculatorService.CalculateTrueAirspeed(request);
        return Ok(result);
    }

    /// <summary>
    /// Estimates cloud base height AGL from surface temperature and dewpoint spread.
    ///
    /// <para>
    /// Uses the standard pilot rule of thumb: <c>cloud base (ft AGL) = (temperature - dewpoint) * 400</c>.
    /// This approximates the lifting condensation level based on the average dry adiabatic lapse rate
    /// (~3°C/1000 ft) and dewpoint lapse rate (~0.5°C/1000 ft).
    /// </para>
    ///
    /// <para><strong>Response Fields</strong></para>
    /// <list type="bullet">
    ///   <item>
    ///     <description><c>EstimatedCloudBaseFtAgl</c> — estimated height of the cloud base above ground level (feet)</description>
    ///   </item>
    ///   <item>
    ///     <description><c>TemperatureDewpointSpreadCelsius</c> — the difference between temperature and dewpoint (degrees Celsius)</description>
    ///   </item>
    /// </list>
    ///
    /// <para>
    /// <em>Note:</em> Actual cloud bases vary with humidity profiles, inversions, and local convective
    /// conditions. A small spread (less than 3°C) generally indicates a high likelihood of low ceilings or fog.
    /// </para>
    /// </summary>
    /// <param name="request">
    /// <c>TemperatureCelsius</c> (surface temperature in °C) and
    /// <c>DewpointCelsius</c> (dewpoint in °C, must be less than or equal to the temperature).
    /// </param>
    /// <returns>Estimated cloud base in feet AGL and the temperature/dewpoint spread</returns>
    /// <response code="200">Returns the cloud base estimation</response>
    /// <response code="400">The dewpoint exceeds the temperature, which is physically invalid</response>
    [HttpPost("cloud-base/calculate")]
    [ProducesResponseType(typeof(CloudBaseResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<CloudBaseResponseDto> CalculateCloudBase(
        [FromBody] CloudBaseRequestDto request)
    {
        var result = e6bCalculatorService.CalculateCloudBase(request);
        return Ok(result);
    }

    /// <summary>
    /// Calculates pressure altitude from field elevation and altimeter setting.
    ///
    /// <para>
    /// Pressure altitude is the altitude in the standard atmosphere where the pressure equals the
    /// current pressure at your location. It is the starting point for density altitude, TAS,
    /// and performance chart calculations.
    /// </para>
    ///
    /// <para>
    /// <em>Formula:</em> <c>PA = FieldElevation + (29.92 - Altimeter) * 1000</c>
    /// </para>
    ///
    /// <para><strong>Response Fields</strong></para>
    /// <list type="bullet">
    ///   <item>
    ///     <description><c>PressureAltitudeFt</c> — the calculated pressure altitude (feet)</description>
    ///   </item>
    ///   <item>
    ///     <description><c>AltimeterCorrectionFt</c> — the deviation from standard pressure expressed in feet (positive = lower pressure than standard, negative = higher)</description>
    ///   </item>
    /// </list>
    ///
    /// <para>
    /// <em>Note:</em> The 1 inHg = 1000 ft approximation is most accurate near sea level and diverges
    /// slightly at higher elevations and extreme altimeter settings.
    /// </para>
    /// </summary>
    /// <param name="request">
    /// <c>FieldElevationFt</c> (feet MSL) and
    /// <c>AltimeterInHg</c> (inches of mercury, must be between 25.0 and 35.0).
    /// </param>
    /// <returns>Pressure altitude and altimeter correction in feet</returns>
    /// <response code="200">Returns the pressure altitude calculation</response>
    /// <response code="400">The altimeter setting is outside the valid range (25.0 - 35.0 inHg)</response>
    [HttpPost("pressure-altitude/calculate")]
    [ProducesResponseType(typeof(PressureAltitudeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<PressureAltitudeResponseDto> CalculatePressureAltitude(
        [FromBody] PressureAltitudeRequestDto request)
    {
        var result = e6bCalculatorService.CalculatePressureAltitude(request);
        return Ok(result);
    }
}

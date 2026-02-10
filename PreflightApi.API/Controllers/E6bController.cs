using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Infrastructure.Dtos.Performance;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

/// <summary>
/// Provides E6B flight computer calculations for VFR pilots, including crosswind components,
/// density altitude, wind triangle, true airspeed, cloud base estimation, and pressure altitude.
/// Airport-based calculations automatically use the latest METAR data for wind, temperature, and altimeter settings.
/// Manual calculation endpoints allow you to provide your own parameters.
/// <para>
/// <b>DISCLAIMER:</b> These calculations are intended for PRE-FLIGHT planning purposes only and must not
/// be used for in-flight navigation. Results are approximations based on the ICAO Standard
/// Atmosphere (ISA) model, which assumes mid-latitude atmospheric conditions. The actual tropopause
/// altitude varies from ~26,000 ft at the poles to ~55,000 ft at the equator, and real atmospheric
/// conditions differ from the ISA model. Always verify against certified instruments and official
/// flight planning tools.
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
    /// Calculates crosswind and headwind components for all runways at an airport using the latest METAR wind data.
    /// Returns components for each runway end and recommends the best runway (lowest crosswind with a headwind).
    /// Requires the airport to have a current METAR with wind data and at least one runway with heading information.
    /// </summary>
    /// <param name="icaoCodeOrIdent">ICAO code or airport identifier</param>
    /// <returns>Crosswind data for all runway ends with recommended runway</returns>
    /// <response code="200">Returns crosswind data for all runways</response>
    /// <response code="400">If METAR is missing required wind data</response>
    /// <response code="404">If the airport or METAR is not found</response>
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
    /// Calculates crosswind and headwind components using manually provided wind and runway heading values.
    /// Useful when you want to calculate components for specific conditions rather than using live METAR data.
    /// </summary>
    /// <param name="request">Wind direction (degrees), wind speed (knots), optional gust speed (knots), and runway heading (magnetic degrees)</param>
    /// <returns>Calculated crosswind and headwind components in knots (positive crosswind = from right, positive headwind = headwind)</returns>
    /// <response code="200">Returns calculated crosswind components</response>
    /// <response code="400">If the request parameters are invalid</response>
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
    /// Calculates density altitude for an airport using the latest METAR temperature and altimeter data.
    /// Optionally override the temperature or altimeter setting (e.g., for "what if" scenarios).
    /// Returns density altitude, pressure altitude, ISA temperature, and temperature deviation.
    /// </summary>
    /// <param name="icaoCodeOrIdent">ICAO code or FAA identifier (e.g., KDFW, DFW)</param>
    /// <param name="request">Optional overrides: temperatureCelsiusOverride and/or altimeterInHgOverride. If not provided, values are taken from the latest METAR.</param>
    /// <returns>Density altitude, pressure altitude, ISA temperature, and deviation from standard</returns>
    /// <response code="200">Returns density altitude data</response>
    /// <response code="400">If METAR is missing required data and no override provided</response>
    /// <response code="404">If the airport or METAR is not found</response>
    /// <remarks>
    /// Density altitude is calculated using the ISA (International Standard Atmosphere) model approximation:
    /// DA = PA + 120 × (OAT − ISA_temp). Real atmospheric density varies with humidity, local pressure
    /// patterns, and non-standard lapse rates that this model does not account for.
    /// </remarks>
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
    /// Calculates density altitude using manually provided field elevation, altimeter setting, and temperature.
    /// Useful for any location or for calculating with non-current weather conditions.
    /// </summary>
    /// <param name="request">Field elevation (feet MSL), altimeter setting (inches of mercury), and temperature (degrees Celsius)</param>
    /// <returns>Density altitude, pressure altitude, ISA temperature, and deviation from standard</returns>
    /// <response code="200">Returns calculated density altitude</response>
    /// <response code="400">If the request parameters are invalid</response>
    /// <remarks>
    /// Density altitude is calculated using the ISA (International Standard Atmosphere) model approximation:
    /// DA = PA + 120 × (OAT − ISA_temp). Real atmospheric density varies with humidity, local pressure
    /// patterns, and non-standard lapse rates that this model does not account for.
    /// </remarks>
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
    /// Calculates true heading and ground speed using the wind triangle.
    /// Given true course, true airspeed, wind direction, and wind speed, returns the wind correction angle,
    /// true heading, ground speed, and headwind/crosswind components.
    /// </summary>
    /// <param name="request">True course (degrees), TAS (knots), wind direction (degrees), wind speed (knots)</param>
    /// <returns>True heading, ground speed, WCA, and wind components</returns>
    /// <response code="200">Returns wind triangle calculation results</response>
    /// <response code="400">If the request parameters are invalid</response>
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
    /// Uses the full compressible isentropic flow conversion (CAS → impact pressure → Mach → TAS),
    /// accurate from sea level through FL410+ including above the tropopause (36,089 ft).
    /// Also returns density altitude and Mach number at the given conditions.
    /// Reference: ICAO Doc 7488 (Standard Atmosphere), isentropic flow relations.
    /// </summary>
    /// <param name="request">Calibrated airspeed (knots), pressure altitude (feet), OAT (°C)</param>
    /// <returns>True airspeed, density altitude, and Mach number</returns>
    /// <response code="200">Returns TAS calculation results</response>
    /// <response code="400">If the request parameters are invalid</response>
    /// <remarks>
    /// This calculation assumes the ISA tropopause at 36,089 ft. The real tropopause varies from
    /// ~26,000 ft near the poles to ~55,000 ft near the equator. Pressure ratio and temperature
    /// model transitions at this boundary affect TAS accuracy at high altitudes in non-mid-latitude regions.
    /// </remarks>
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
    /// Estimates cloud base height AGL from surface temperature and dewpoint.
    /// Uses the standard spread × 400 formula (equivalent to spread / 2.5 × 1000).
    /// </summary>
    /// <param name="request">Surface temperature (°C) and dewpoint (°C)</param>
    /// <returns>Estimated cloud base in feet AGL and temperature/dewpoint spread</returns>
    /// <response code="200">Returns cloud base estimation</response>
    /// <response code="400">If dewpoint exceeds temperature</response>
    /// <remarks>
    /// This estimation uses the average dry adiabatic lapse rate (~3°C/1000 ft) and dewpoint lapse
    /// rate (~0.5°C/1000 ft) to approximate the lifting condensation level. Actual cloud bases vary
    /// with humidity profiles, inversions, and local convective conditions.
    /// </remarks>
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
    /// PA = field elevation + (29.92 − altimeter) × 1000.
    /// </summary>
    /// <param name="request">Field elevation (feet MSL) and altimeter setting (inHg)</param>
    /// <returns>Pressure altitude and altimeter correction in feet</returns>
    /// <response code="200">Returns pressure altitude calculation</response>
    /// <response code="400">If the altimeter setting is out of range</response>
    /// <remarks>
    /// This uses the standard altimetry relationship from the ICAO Standard Atmosphere. The 1 inHg ≈ 1000 ft
    /// approximation is most accurate near sea level and diverges slightly at higher altitudes and extreme
    /// altimeter settings.
    /// </remarks>
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

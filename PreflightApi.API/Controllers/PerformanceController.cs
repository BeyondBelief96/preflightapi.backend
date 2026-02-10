using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PreflightApi.API.Models;
using PreflightApi.Infrastructure.Dtos.Performance;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Controllers;

/// <summary>
/// Provides aviation performance calculations including crosswind components and density altitude.
/// Airport-based calculations automatically use the latest METAR data for wind, temperature, and altimeter settings.
/// Manual calculation endpoints allow you to provide your own parameters.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/performance")]
[Tags("Performance Calculations")]
public class PerformanceController(IPerformanceCalculatorService performanceCalculatorService)
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
        var result = await performanceCalculatorService.GetCrosswindForAirportAsync(icaoCodeOrIdent);
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
        var result = performanceCalculatorService.CalculateCrosswind(request);
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
    [HttpGet("density-altitude/{icaoCodeOrIdent}")]
    [ProducesResponseType(typeof(DensityAltitudeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<DensityAltitudeResponseDto>> GetDensityAltitudeForAirport(
        string icaoCodeOrIdent,
        [FromQuery] AirportDensityAltitudeRequestDto? request = null)
    {
        var result = await performanceCalculatorService.GetDensityAltitudeForAirportAsync(
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
    [HttpPost("density-altitude/calculate")]
    [ProducesResponseType(typeof(DensityAltitudeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<DensityAltitudeResponseDto> CalculateDensityAltitude(
        [FromBody] DensityAltitudeRequestDto request)
    {
        var result = performanceCalculatorService.CalculateDensityAltitude(request);
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
        var result = performanceCalculatorService.CalculateWindTriangle(request);
        return Ok(result);
    }

    /// <summary>
    /// Calculates true airspeed (TAS) from calibrated airspeed, pressure altitude, and outside air temperature.
    /// Also returns density altitude and Mach number at the given conditions.
    /// </summary>
    /// <param name="request">Calibrated airspeed (knots), pressure altitude (feet), OAT (°C)</param>
    /// <returns>True airspeed, density altitude, and Mach number</returns>
    /// <response code="200">Returns TAS calculation results</response>
    /// <response code="400">If the request parameters are invalid</response>
    [HttpPost("true-airspeed/calculate")]
    [ProducesResponseType(typeof(TrueAirspeedResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<TrueAirspeedResponseDto> CalculateTrueAirspeed(
        [FromBody] TrueAirspeedRequestDto request)
    {
        var result = performanceCalculatorService.CalculateTrueAirspeed(request);
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
    [HttpPost("cloud-base/calculate")]
    [ProducesResponseType(typeof(CloudBaseResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<CloudBaseResponseDto> CalculateCloudBase(
        [FromBody] CloudBaseRequestDto request)
    {
        var result = performanceCalculatorService.CalculateCloudBase(request);
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
    [HttpPost("pressure-altitude/calculate")]
    [ProducesResponseType(typeof(PressureAltitudeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<PressureAltitudeResponseDto> CalculatePressureAltitude(
        [FromBody] PressureAltitudeRequestDto request)
    {
        var result = performanceCalculatorService.CalculatePressureAltitude(request);
        return Ok(result);
    }
}

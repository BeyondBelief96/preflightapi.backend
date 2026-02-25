using PreflightApi.Infrastructure.Dtos.Performance;

namespace PreflightApi.Infrastructure.Interfaces;

public interface IE6bCalculatorService
{
    /// <summary>
    /// Calculates crosswind components for all runways at an airport using current METAR data
    /// </summary>
    /// <param name="icaoCodeOrIdent">ICAO code or airport identifier</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Crosswind data for all runway ends</returns>
    Task<AirportCrosswindResponseDto> GetCrosswindForAirportAsync(string icaoCodeOrIdent, CancellationToken ct = default);

    /// <summary>
    /// Calculates crosswind components using manual parameters
    /// </summary>
    /// <param name="request">Crosswind calculation parameters</param>
    /// <returns>Calculated crosswind components</returns>
    CrosswindCalculationResponseDto CalculateCrosswind(CrosswindCalculationRequestDto request);

    /// <summary>
    /// Calculates density altitude for an airport using current METAR data
    /// </summary>
    /// <param name="icaoCodeOrIdent">ICAO code or airport identifier</param>
    /// <param name="request">Optional parameter overrides</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Density altitude calculation result</returns>
    Task<DensityAltitudeResponseDto> GetDensityAltitudeForAirportAsync(
        string icaoCodeOrIdent,
        AirportDensityAltitudeRequestDto? request = null,
        CancellationToken ct = default);

    /// <summary>
    /// Calculates density altitude using manual parameters
    /// </summary>
    /// <param name="request">Density altitude calculation parameters</param>
    /// <returns>Calculated density altitude</returns>
    DensityAltitudeResponseDto CalculateDensityAltitude(DensityAltitudeRequestDto request);

    /// <summary>
    /// Calculates wind correction angle, true heading, and ground speed from a wind triangle
    /// </summary>
    /// <param name="request">Wind triangle calculation parameters</param>
    /// <returns>Heading, ground speed, and wind components</returns>
    WindTriangleResponseDto CalculateWindTriangle(WindTriangleRequestDto request);

    /// <summary>
    /// Calculates true airspeed from calibrated airspeed, pressure altitude, and temperature
    /// </summary>
    /// <param name="request">True airspeed calculation parameters</param>
    /// <returns>TAS, density altitude, and Mach number</returns>
    TrueAirspeedResponseDto CalculateTrueAirspeed(TrueAirspeedRequestDto request);

    /// <summary>
    /// Estimates cloud base height AGL from temperature/dewpoint spread
    /// </summary>
    /// <param name="request">Cloud base estimation parameters</param>
    /// <returns>Estimated cloud base in feet AGL</returns>
    CloudBaseResponseDto CalculateCloudBase(CloudBaseRequestDto request);

    /// <summary>
    /// Calculates pressure altitude from field elevation and altimeter setting
    /// </summary>
    /// <param name="request">Pressure altitude calculation parameters</param>
    /// <returns>Pressure altitude and altimeter correction</returns>
    PressureAltitudeResponseDto CalculatePressureAltitude(PressureAltitudeRequestDto request);
}

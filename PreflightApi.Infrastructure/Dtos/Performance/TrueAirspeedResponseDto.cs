namespace PreflightApi.Infrastructure.Dtos.Performance;

/// <summary>
/// Response DTO for true airspeed calculation.
/// </summary>
public record TrueAirspeedResponseDto
{
    /// <summary>
    /// Calculated true airspeed in knots
    /// </summary>
    public double TrueAirspeedKt { get; init; }

    /// <summary>
    /// Density altitude in feet at the given conditions
    /// </summary>
    public double DensityAltitudeFt { get; init; }

    /// <summary>
    /// Mach number at the given conditions
    /// </summary>
    public double MachNumber { get; init; }

    /// <summary>
    /// Calibrated airspeed used in calculation (knots)
    /// </summary>
    public double CalibratedAirspeedKt { get; init; }

    /// <summary>
    /// Pressure altitude used in calculation (feet)
    /// </summary>
    public double PressureAltitudeFt { get; init; }

    /// <summary>
    /// Outside air temperature used in calculation (°C)
    /// </summary>
    public double OutsideAirTemperatureCelsius { get; init; }
}

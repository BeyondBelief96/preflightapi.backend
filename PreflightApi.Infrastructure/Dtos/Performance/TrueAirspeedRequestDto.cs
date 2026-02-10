namespace PreflightApi.Infrastructure.Dtos.Performance;

/// <summary>
/// Request DTO for true airspeed (TAS) calculation.
/// </summary>
public record TrueAirspeedRequestDto
{
    /// <summary>
    /// Calibrated (indicated) airspeed in knots (must be greater than 0)
    /// </summary>
    public double CalibratedAirspeedKt { get; init; }

    /// <summary>
    /// Pressure altitude in feet (can be negative, e.g. Death Valley)
    /// </summary>
    public double PressureAltitudeFt { get; init; }

    /// <summary>
    /// Outside air temperature in degrees Celsius
    /// </summary>
    public double OutsideAirTemperatureCelsius { get; init; }
}

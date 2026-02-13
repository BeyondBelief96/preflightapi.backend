namespace PreflightApi.Infrastructure.Dtos.Performance;

/// <summary>
/// Response DTO for standalone pressure altitude calculation.
/// </summary>
public record PressureAltitudeResponseDto
{
    /// <summary>
    /// Calculated pressure altitude in feet
    /// </summary>
    public double PressureAltitudeFt { get; init; }

    /// <summary>
    /// Altimeter correction — deviation from standard pressure expressed in feet
    /// </summary>
    public double AltimeterCorrectionFt { get; init; }

    /// <summary>
    /// Field elevation used in calculation (feet MSL)
    /// </summary>
    public double FieldElevationFt { get; init; }

    /// <summary>
    /// Altimeter setting used in calculation (inHg)
    /// </summary>
    public double AltimeterInHg { get; init; }
}

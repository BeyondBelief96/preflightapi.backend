namespace PreflightApi.Infrastructure.Dtos.Performance;

/// <summary>
/// Request DTO for standalone pressure altitude calculation.
/// </summary>
public record PressureAltitudeRequestDto
{
    /// <summary>
    /// Field elevation in feet MSL
    /// </summary>
    public double FieldElevationFt { get; init; }

    /// <summary>
    /// Altimeter setting in inches of mercury (must be between 25.0 and 35.0 inHg)
    /// </summary>
    public double AltimeterInHg { get; init; }
}

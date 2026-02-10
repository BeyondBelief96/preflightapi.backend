namespace PreflightApi.Infrastructure.Dtos.Performance;

/// <summary>
/// Request DTO for wind triangle (heading and ground speed) calculation.
/// </summary>
public record WindTriangleRequestDto
{
    /// <summary>
    /// True course in degrees (0–360)
    /// </summary>
    public double TrueCourseDegrees { get; init; }

    /// <summary>
    /// True airspeed in knots (must be greater than 0)
    /// </summary>
    public double TrueAirspeedKt { get; init; }

    /// <summary>
    /// Wind direction in degrees (0–360)
    /// </summary>
    public double WindDirectionDegrees { get; init; }

    /// <summary>
    /// Wind speed in knots (must be ≥ 0)
    /// </summary>
    public double WindSpeedKt { get; init; }
}

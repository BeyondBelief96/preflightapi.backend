namespace PreflightApi.Infrastructure.Dtos.Performance;

/// <summary>
/// Response DTO for wind triangle calculation containing heading and ground speed results.
/// </summary>
public record WindTriangleResponseDto
{
    /// <summary>
    /// True heading in degrees (0–360), corrected for wind
    /// </summary>
    public double TrueHeadingDegrees { get; init; }

    /// <summary>
    /// Ground speed in knots
    /// </summary>
    public double GroundSpeedKt { get; init; }

    /// <summary>
    /// Wind correction angle in degrees (positive = right correction, negative = left)
    /// </summary>
    public double WindCorrectionAngleDegrees { get; init; }

    /// <summary>
    /// Headwind component in knots (positive = headwind, negative = tailwind)
    /// </summary>
    public double HeadwindComponentKt { get; init; }

    /// <summary>
    /// Crosswind component in knots (positive = from the right, negative = from the left)
    /// </summary>
    public double CrosswindComponentKt { get; init; }

    /// <summary>
    /// True course used in calculation (degrees)
    /// </summary>
    public double TrueCourseDegrees { get; init; }

    /// <summary>
    /// True airspeed used in calculation (knots)
    /// </summary>
    public double TrueAirspeedKt { get; init; }

    /// <summary>
    /// Wind direction used in calculation (degrees)
    /// </summary>
    public double WindDirectionDegrees { get; init; }

    /// <summary>
    /// Wind speed used in calculation (knots)
    /// </summary>
    public double WindSpeedKt { get; init; }
}

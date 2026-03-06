namespace PreflightApi.Infrastructure.Dtos.Navlog;

/// <summary>
/// Result of a bearing and distance calculation between two points.
/// </summary>
public record BearingAndDistanceResponseDto
{
    /// <summary>True course in degrees.</summary>
    public double TrueCourse { get; init; }
    /// <summary>Magnetic course in degrees (adjusted for magnetic variation).</summary>
    public double MagneticCourse { get; init; }
    /// <summary>Great-circle distance in nautical miles.</summary>
    public double Distance { get; init; }
}

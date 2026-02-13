namespace PreflightApi.Infrastructure.Dtos.Navlog;

/// <summary>
/// Request to calculate bearing and distance between two geographic points.
/// </summary>
public record BearingAndDistanceRequestDto
{
    /// <summary>Starting point latitude in decimal degrees.</summary>
    public double StartLatitude { get; init; }
    /// <summary>Starting point longitude in decimal degrees.</summary>
    public double StartLongitude { get; init; }
    /// <summary>Ending point latitude in decimal degrees.</summary>
    public double EndLatitude { get; init; }
    /// <summary>Ending point longitude in decimal degrees.</summary>
    public double EndLongitude { get; init; }
}

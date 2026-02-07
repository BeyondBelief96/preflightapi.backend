using PreflightApi.Domain.Enums;

namespace PreflightApi.Infrastructure.Dtos;

/// <summary>
/// Obstacle data from the FAA Digital Obstacle File.
/// </summary>
public record ObstacleDto
{
    /// <summary>Obstacle Assessment Surface number (unique identifier).</summary>
    public string OasNumber { get; init; } = string.Empty;
    /// <summary>Two-letter state identifier.</summary>
    public string? StateId { get; init; }
    /// <summary>City nearest to the obstacle.</summary>
    public string? CityName { get; init; }
    /// <summary>Latitude in decimal degrees.</summary>
    public decimal? Latitude { get; init; }
    /// <summary>Longitude in decimal degrees.</summary>
    public decimal? Longitude { get; init; }
    /// <summary>Type of obstacle (e.g., TOWER, BLDG, STACK).</summary>
    public string? ObstacleType { get; init; }
    /// <summary>Number of obstacles at this location.</summary>
    public int? Quantity { get; init; }
    /// <summary>Height above ground level in feet.</summary>
    public int? HeightAgl { get; init; }
    /// <summary>Height above mean sea level in feet.</summary>
    public int? HeightAmsl { get; init; }
    /// <summary>Lighting type on the obstacle.</summary>
    public ObstacleLighting Lighting { get; init; }
    /// <summary>Horizontal accuracy of the position data.</summary>
    public HorizontalAccuracy HorizontalAccuracy { get; init; }
    /// <summary>Vertical accuracy of the height data.</summary>
    public VerticalAccuracy VerticalAccuracy { get; init; }
    /// <summary>Marking type on the obstacle.</summary>
    public ObstacleMarking Marking { get; init; }
    /// <summary>Verification status of the obstacle data.</summary>
    public VerificationStatus VerificationStatus { get; init; }
}

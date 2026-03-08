using PreflightApi.Domain.Enums;

namespace PreflightApi.Infrastructure.Dtos;

/// <summary>
/// Obstacle data from the FAA Digital Obstacle File (DOF).
/// Use the OasNumber to cross-reference with navigation log results — the navlog response's
/// ObstacleOasNumbers field contains OAS numbers from this endpoint for obstacles near a planned route.
/// </summary>
public record ObstacleDto
{
    /// <summary>Obstacle Assessment Surface (OAS) number — the unique identifier for this obstacle. This is the key used to look up obstacles returned by the navigation log endpoint's ObstacleOasNumbers field.</summary>
    public string OasNumber { get; init; } = string.Empty;
    /// <summary>Two-letter state identifier.</summary>
    public string? StateId { get; init; }
    /// <summary>City nearest to the obstacle.</summary>
    public string? CityName { get; init; }
    /// <summary>Latitude in decimal degrees (WGS 84).</summary>
    public double? Latitude { get; init; }
    /// <summary>Longitude in decimal degrees (WGS 84).</summary>
    public double? Longitude { get; init; }
    /// <summary>Type of obstacle (e.g., TOWER, BLDG, STACK).</summary>
    public string? ObstacleType { get; init; }
    /// <summary>Number of obstacles at this location.</summary>
    public int? Quantity { get; init; }
    /// <summary>Height above ground level in feet.</summary>
    public int? HeightAgl { get; init; }
    /// <summary>Height above mean sea level in feet.</summary>
    public int? HeightAmsl { get; init; }
    /// <summary>Lighting type on the obstacle.</summary>
    public ObstacleLighting? Lighting { get; init; }
    /// <summary>Horizontal accuracy of the position data.</summary>
    public HorizontalAccuracy? HorizontalAccuracy { get; init; }
    /// <summary>Vertical accuracy of the height data.</summary>
    public VerticalAccuracy? VerticalAccuracy { get; init; }
    /// <summary>Marking type on the obstacle.</summary>
    public ObstacleMarking? Marking { get; init; }
    /// <summary>Verification status of the obstacle data.</summary>
    public VerificationStatus? VerificationStatus { get; init; }
}

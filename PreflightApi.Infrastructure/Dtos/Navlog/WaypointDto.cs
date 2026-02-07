using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Enums;

namespace PreflightApi.Infrastructure.Dtos.Navlog;

/// <summary>
/// A waypoint along a navigation route.
/// </summary>
public record WaypointDto
{
    /// <summary>Waypoint identifier (e.g., ICAO code or custom name).</summary>
    public string Id { get; init; } = string.Empty;
    /// <summary>Display name of the waypoint.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Latitude in decimal degrees.</summary>
    public double Latitude { get; init; }
    /// <summary>Longitude in decimal degrees.</summary>
    public double Longitude { get; init; }
    /// <summary>Altitude in feet MSL.</summary>
    public double Altitude { get; init; }
    /// <summary>Type of waypoint (e.g., airport, navaid, user-defined).</summary>
    public WaypointType? WaypointType { get; init; }
    /// <summary>Gallons to add during a refueling stop.</summary>
    public double? RefuelGallons { get; init; }
    /// <summary>Whether to refuel to full capacity at this stop.</summary>
    public bool? RefuelToFull { get; init; }
    /// <summary>Whether this waypoint is a refueling stop.</summary>
    public bool? IsRefuelingStop { get; init; }
}

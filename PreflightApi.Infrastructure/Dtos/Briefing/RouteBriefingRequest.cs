namespace PreflightApi.Infrastructure.Dtos.Briefing;

/// <summary>
/// Request for a composite route weather briefing along a flight path.
/// </summary>
public record RouteBriefingRequest
{
    /// <summary>
    /// Ordered list of waypoints defining the flight route. Each waypoint is either an
    /// airport identifier (ICAO code or FAA identifier) or a lat/lon coordinate pair.
    /// At least two waypoints are required to define a route.
    /// </summary>
    public List<BriefingWaypoint> Waypoints { get; init; } = [];

    /// <summary>
    /// Corridor width in nautical miles on each side of the route centerline.
    /// Used for PIREPs, NOTAMs, and airport searches. Default is 25 NM.
    /// </summary>
    public double CorridorWidthNm { get; init; } = 25;
}

/// <summary>
/// A single waypoint in a briefing route. Specify either an airport identifier
/// or latitude/longitude coordinates.
/// </summary>
public record BriefingWaypoint
{
    /// <summary>
    /// Airport ICAO code (e.g., KDFW) or FAA identifier (e.g., DFW).
    /// If provided, latitude and longitude are ignored and looked up from the database.
    /// </summary>
    public string? AirportIdentifier { get; init; }

    /// <summary>Latitude in decimal degrees (-90 to 90). Required when AirportIdentifier is null.</summary>
    public decimal? Latitude { get; init; }

    /// <summary>Longitude in decimal degrees (-180 to 180). Required when AirportIdentifier is null.</summary>
    public decimal? Longitude { get; init; }

    /// <summary>Whether this waypoint specifies an airport identifier.</summary>
    public bool IsAirport => !string.IsNullOrWhiteSpace(AirportIdentifier);
}

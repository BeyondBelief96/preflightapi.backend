namespace PreflightApi.Infrastructure.Dtos.Notam;

/// <summary>
/// Request DTO for querying NOTAMs along a flight route
/// </summary>
public record NotamQueryByRouteRequest
{
    /// <summary>
    /// List of airport identifiers (ICAO codes or FAA identifiers) along the route.
    /// Use this for simple airport-only queries. For mixed airport/waypoint routes, use RoutePoints instead.
    /// </summary>
    public List<string> AirportIdentifiers { get; init; } = [];

    /// <summary>
    /// Ordered list of route points (airports and/or waypoints) in flight sequence.
    /// Each point can be either an airport (by identifier) or a waypoint (by lat/lon).
    /// If both AirportIdentifiers and RoutePoints are provided, RoutePoints takes precedence.
    /// </summary>
    public List<RoutePointDto> RoutePoints { get; init; } = [];

    /// <summary>
    /// Default radius in nautical miles for waypoints without a specific radius.
    /// If not specified, uses default from settings.
    /// </summary>
    public double? CorridorRadiusNm { get; init; }

    /// <summary>
    /// Optional NMS query filters applied to all route point queries.
    /// </summary>
    public NotamFilterDto? Filters { get; init; }
}

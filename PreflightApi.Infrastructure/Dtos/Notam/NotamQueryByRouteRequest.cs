namespace PreflightApi.Infrastructure.Dtos.Notam;

/// <summary>
/// Request body for querying NOTAMs along a flight route. Provide either <c>AirportIdentifiers</c>
/// (simple airport-only routes) or <c>RoutePoints</c> (mixed airports and waypoints).
/// If both are provided, <c>RoutePoints</c> is used and <c>AirportIdentifiers</c> is ignored.
/// NOTAMs from all route points are combined and deduplicated in the response.
/// </summary>
public record NotamQueryByRouteRequest
{
    /// <summary>
    /// Airport identifiers (ICAO codes or FAA identifiers) along the route, e.g. ["KDFW", "KAUS"].
    /// Each airport is queried by identifier match. Use this when your entire route is airport-to-airport
    /// with no en-route waypoints. Ignored when <c>RoutePoints</c> is provided.
    /// </summary>
    public List<string> AirportIdentifiers { get; init; } = [];

    /// <summary>
    /// Route points in flight order. Each point is either an airport (by identifier) or a geographic
    /// waypoint (by lat/lon with radius search). Use this when your route includes en-route waypoints
    /// or you need per-point radius control. When provided, <c>AirportIdentifiers</c> is ignored.
    /// </summary>
    public List<RoutePointDto> RoutePoints { get; init; } = [];

    /// <summary>
    /// Default search radius in nautical miles applied to any waypoint in <c>RoutePoints</c> that does not
    /// specify its own <c>RadiusNm</c>. Has no effect on airport points or on <c>AirportIdentifiers</c>.
    /// If omitted, the server default (25 NM) is used.
    /// </summary>
    public double? CorridorRadiusNm { get; init; }

    /// <summary>
    /// Optional filters (classification, feature, freeText, date range) applied to every route point query.
    /// </summary>
    public NotamFilterDto? Filters { get; init; }
}

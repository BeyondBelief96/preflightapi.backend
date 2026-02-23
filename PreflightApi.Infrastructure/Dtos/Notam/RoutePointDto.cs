namespace PreflightApi.Infrastructure.Dtos.Notam;

/// <summary>
/// A single point along a flight route. Each point is either an airport or a geographic waypoint:
/// <list type="bullet">
///   <item><description><strong>Airport point</strong>: Set <c>AirportIdentifier</c>. NOTAMs are queried by identifier match (not spatial).</description></item>
///   <item><description><strong>Waypoint</strong>: Set <c>Latitude</c> and <c>Longitude</c>. NOTAMs are queried by spatial radius search.</description></item>
/// </list>
/// </summary>
public record RoutePointDto
{
    /// <summary>
    /// ICAO code (e.g., "KDFW") or FAA identifier (e.g., "DFW"). Case-insensitive.
    /// If set, this point is treated as an airport and NOTAMs are matched by identifier.
    /// Latitude, Longitude, and RadiusNm are ignored for airport points.
    /// </summary>
    public string? AirportIdentifier { get; init; }

    /// <summary>
    /// Optional display name for waypoints (e.g., "Lake Travis", "MAVER").
    /// Appears in the response's <c>queryLocation</c> route description.
    /// If omitted, coordinates are used instead (e.g., "30.4082N, 97.8538W").
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Latitude in decimal degrees (-90 to 90). Required when <c>AirportIdentifier</c> is not set.
    /// </summary>
    public double? Latitude { get; init; }

    /// <summary>
    /// Longitude in decimal degrees (-180 to 180). Required when <c>AirportIdentifier</c> is not set.
    /// </summary>
    public double? Longitude { get; init; }

    /// <summary>
    /// Search radius in nautical miles for this waypoint (max 100). Only applies to waypoints.
    /// If omitted, falls back to the request-level <c>CorridorRadiusNm</c>, then the server default (25 NM).
    /// </summary>
    public double? RadiusNm { get; init; }

}

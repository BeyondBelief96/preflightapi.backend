namespace PreflightApi.Infrastructure.Dtos.Navlog;

/// <summary>
/// Complete navigation log calculation result for a VFR cross-country flight.
/// Includes per-leg calculations (course, speed, fuel, wind) and references to airspace and obstacle
/// data along the route. Use the returned identifiers with the Airspace and Obstacle endpoints to
/// retrieve full details about en-route hazards.
/// </summary>
public record NavlogResponseDto
{
    /// <summary>Total route distance in nautical miles (sum of all leg distances).</summary>
    public double TotalRouteDistance { get; init; }
    /// <summary>Total estimated en-route time in hours (including climb and descent phases).</summary>
    public double TotalRouteTimeHours { get; init; }
    /// <summary>Total fuel consumed for the entire route in gallons (including start/taxi/takeoff fuel).</summary>
    public double TotalFuelUsed { get; init; }
    /// <summary>Average wind component across all legs in knots. Negative values indicate a net headwind; positive values indicate a net tailwind.</summary>
    public double AverageWindComponent { get; init; }
    /// <summary>Ordered list of navigation legs between each pair of consecutive waypoints, containing computed course, speed, fuel, and wind data.</summary>
    public List<NavigationLegDto> Legs { get; init; } = [];
    /// <summary>
    /// Global IDs of controlled airspaces (Class B, C, D, E) that the route passes through.
    /// Use these IDs with the <c>GET /api/v1/airspaces/by-global-ids?globalIds=...</c> endpoint to retrieve full airspace details including boundaries and altitude limits.
    /// </summary>
    public IReadOnlyCollection<string> AirspaceGlobalIds { get; init; } = Array.Empty<string>();
    /// <summary>
    /// Global IDs of special use airspaces (restricted, prohibited, MOA, warning, alert) that the route passes through.
    /// Use these IDs with the <c>GET /api/v1/airspaces/special-use/by-global-ids?globalIds=...</c> endpoint to retrieve full airspace details including boundaries and altitude limits.
    /// </summary>
    public IReadOnlyCollection<string> SpecialUseAirspaceGlobalIds { get; init; } = Array.Empty<string>();
    /// <summary>
    /// OAS (Obstacle Assessment Surface) numbers of obstacles near the route.
    /// Use these numbers with the <c>POST /api/v1/obstacles/by-oas-numbers</c> endpoint to retrieve full obstacle details including type, height, and lighting.
    /// </summary>
    public IReadOnlyCollection<string> ObstacleOasNumbers { get; init; } = Array.Empty<string>();
}

namespace PreflightApi.Infrastructure.Dtos.Navlog;

/// <summary>
/// Complete navigation log calculation result for a cross-country flight.
/// </summary>
public record NavlogResponseDto
{
    /// <summary>Total route distance in nautical miles.</summary>
    public double TotalRouteDistance { get; set; }
    /// <summary>Total route time in hours.</summary>
    public double TotalRouteTimeHours { get; set; }
    /// <summary>Total fuel used for the route in gallons.</summary>
    public double TotalFuelUsed { get; set; }
    /// <summary>Average wind component across all legs (negative = headwind).</summary>
    public double AverageWindComponent { get; set; }
    /// <summary>Individual navigation legs with computed data.</summary>
    public List<NavigationLegDto> Legs { get; set; } = [];
    /// <summary>Global IDs of controlled airspaces along the route.</summary>
    public IReadOnlyCollection<string> AirspaceGlobalIds { get; set; } = Array.Empty<string>();
    /// <summary>Global IDs of special use airspaces along the route.</summary>
    public IReadOnlyCollection<string> SpecialUseAirspaceGlobalIds { get; set; } = Array.Empty<string>();
    /// <summary>OAS numbers of obstacles along the route.</summary>
    public IReadOnlyCollection<string> ObstacleOasNumbers { get; set; } = Array.Empty<string>();
}

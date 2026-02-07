namespace PreflightApi.Infrastructure.Dtos.Navlog;

/// <summary>
/// Aircraft performance data used for navigation log calculations.
/// </summary>
public record NavlogPerformanceDataDto
{
    /// <summary>True airspeed during climb in knots.</summary>
    public int ClimbTrueAirspeed { get; init; }
    /// <summary>True airspeed during cruise in knots.</summary>
    public int CruiseTrueAirspeed { get; init; }
    /// <summary>True airspeed during descent in knots.</summary>
    public int DescentTrueAirspeed { get; init; }
    /// <summary>Rate of climb in feet per minute.</summary>
    public int ClimbFpm { get; init; }
    /// <summary>Rate of descent in feet per minute.</summary>
    public int DescentFpm { get; init; }
    /// <summary>Fuel burn rate during climb in gallons per hour.</summary>
    public double ClimbFuelBurn { get; init; }
    /// <summary>Fuel burn rate during cruise in gallons per hour.</summary>
    public double CruiseFuelBurn { get; init; }
    /// <summary>Fuel burn rate during descent in gallons per hour.</summary>
    public double DescentFuelBurn { get; init; }
    /// <summary>Fuel used during start, taxi, and takeoff in gallons.</summary>
    public double SttFuelGals { get; init; }
    /// <summary>Total fuel on board at departure in gallons.</summary>
    public double FuelOnBoardGals { get; init; }
}

/// <summary>
/// Request to calculate a VFR navigation log for a cross-country flight.
/// </summary>
public record NavlogRequestDto
{
    /// <summary>Ordered list of waypoints defining the route.</summary>
    public List<WaypointDto> Waypoints { get; init; } = [];
    /// <summary>Aircraft performance parameters for the flight.</summary>
    public NavlogPerformanceDataDto PerformanceData { get; init; } = new();
    /// <summary>Planned cruising altitude in feet MSL.</summary>
    public int PlannedCruisingAltitude { get; init; }
    /// <summary>Planned departure time in UTC.</summary>
    public DateTime TimeOfDeparture { get; init; }
}

namespace PreflightApi.Infrastructure.Dtos.Navlog;

public record NavlogPerformanceDataDto
{
    public int ClimbTrueAirspeed { get; init; }
    public int CruiseTrueAirspeed { get; init; }
    public int DescentTrueAirspeed { get; init; }
    public int ClimbFpm { get; init; }
    public int DescentFpm { get; init; }
    public double ClimbFuelBurn { get; init; }
    public double CruiseFuelBurn { get; init; }
    public double DescentFuelBurn { get; init; }
    public double SttFuelGals { get; init; }
    public double FuelOnBoardGals { get; init; }
}

public record NavlogRequestDto
{
    public List<WaypointDto> Waypoints { get; init; } = [];
    public NavlogPerformanceDataDto PerformanceData { get; init; } = new();
    public int PlannedCruisingAltitude { get; init; }
    public DateTime TimeOfDeparture { get; init; }
}

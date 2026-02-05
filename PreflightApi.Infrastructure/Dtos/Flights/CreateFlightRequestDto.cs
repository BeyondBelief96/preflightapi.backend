using PreflightApi.Infrastructure.Dtos.Navlog;

namespace PreflightApi.Infrastructure.Dtos.Flights;

public record CreateFlightRequestDto
{
    public string Name { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public int PlannedCruisingAltitude { get; set; }
    public List<WaypointDto> Waypoints { get; set; } = [];
    public string AircraftPerformanceProfileId { get; set; } = string.Empty;
    public string? AircraftId { get; set; }
}
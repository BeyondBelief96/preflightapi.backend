namespace PreflightApi.Domain.ValueObjects.NavigationalAids;

public class NavaidCheckpoint
{
    public int? Altitude { get; set; }
    public string? Bearing { get; set; }
    public string? AirGroundCode { get; set; }
    public string? Description { get; set; }
    public string? AirportId { get; set; }
    public string? StateCheckCode { get; set; }
}

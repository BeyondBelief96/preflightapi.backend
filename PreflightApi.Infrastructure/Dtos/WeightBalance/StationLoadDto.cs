namespace PreflightApi.Infrastructure.Dtos.WeightBalance;

public record StationLoadDto
{
    public string StationId { get; init; } = string.Empty;
    public double? Weight { get; init; }        // For Standard stations
    public double? FuelGallons { get; init; }   // For Fuel stations
    public double? OilQuarts { get; init; }     // For Oil stations
}

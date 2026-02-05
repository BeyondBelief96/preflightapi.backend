namespace PreflightApi.Infrastructure.Dtos.WeightBalance;

public record WeightBalanceCalculationRequestDto
{
    public List<StationLoadDto> LoadedStations { get; init; } = [];
    public string? EnvelopeId { get; init; }
    public double? FuelBurnGallons { get; init; }
}

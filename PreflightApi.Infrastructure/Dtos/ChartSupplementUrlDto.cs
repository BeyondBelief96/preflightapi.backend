namespace PreflightApi.Infrastructure.Dtos;

public record ChartSupplementDto
{
    public string PdfUrl { get; init; } = string.Empty;
}

public record ChartSupplementsResponseDto
{
    public string? AirportName { get; init; }
    public string? AirportCity { get; init; }
    public string? AirportCode { get; init; }
    public List<ChartSupplementDto> Supplements { get; init; } = new();
}

namespace PreflightApi.Infrastructure.Dtos;

public record ChartSupplementUrlDto
{
    public string PdfUrl { get; init; } = string.Empty;
}
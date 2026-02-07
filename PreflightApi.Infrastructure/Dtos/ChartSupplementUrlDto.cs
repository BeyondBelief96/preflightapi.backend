namespace PreflightApi.Infrastructure.Dtos;

/// <summary>
/// A chart supplement page with a pre-signed URL for PDF access.
/// </summary>
public record ChartSupplementDto
{
    /// <summary>Pre-signed URL to download the chart supplement PDF.</summary>
    public string PdfUrl { get; init; } = string.Empty;
}

/// <summary>
/// Airport information with all available chart supplement pages.
/// </summary>
public record ChartSupplementsResponseDto
{
    /// <summary>Official airport name.</summary>
    public string? AirportName { get; init; }
    /// <summary>City the airport is associated with.</summary>
    public string? AirportCity { get; init; }
    /// <summary>Airport ICAO code or FAA identifier.</summary>
    public string? AirportCode { get; init; }
    /// <summary>List of chart supplement pages with download URLs.</summary>
    public List<ChartSupplementDto> Supplements { get; init; } = new();
}

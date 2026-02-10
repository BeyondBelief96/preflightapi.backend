namespace PreflightApi.Infrastructure.Dtos;

/// <summary>
/// A single chart supplement page with a time-limited pre-signed URL for PDF download.
/// </summary>
public record ChartSupplementDto
{
    /// <summary>Pre-signed URL to download the chart supplement PDF page. This URL expires after a limited time period; request a new URL if it has expired.</summary>
    public string PdfUrl { get; init; } = string.Empty;
}

/// <summary>
/// Airport information with all available chart supplement (formerly Airport/Facility Directory) pages.
/// Chart supplements contain detailed airport information including runway data, lighting, services,
/// NOTAMs, and other operational details published by the FAA. The PDF URLs are time-limited pre-signed URLs.
/// </summary>
public record ChartSupplementsResponseDto
{
    /// <summary>Official airport name.</summary>
    public string? AirportName { get; init; }
    /// <summary>City the airport is associated with.</summary>
    public string? AirportCity { get; init; }
    /// <summary>Airport ICAO code or FAA identifier used to query this data.</summary>
    public string? AirportCode { get; init; }
    /// <summary>List of chart supplement PDF pages with time-limited download URLs. Multi-page supplements will have one entry per page.</summary>
    public List<ChartSupplementDto> Supplements { get; init; } = new();
}

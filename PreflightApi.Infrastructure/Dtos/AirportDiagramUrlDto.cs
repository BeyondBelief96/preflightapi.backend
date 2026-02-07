namespace PreflightApi.Infrastructure.Dtos;

/// <summary>
/// An airport diagram with a pre-signed URL for PDF access.
/// </summary>
public record AirportDiagramDto
{
    /// <summary>Name of the chart/diagram.</summary>
    public string ChartName { get; init; } = string.Empty;
    /// <summary>Pre-signed URL to download the diagram PDF.</summary>
    public string PdfUrl { get; init; } = string.Empty;
}

/// <summary>
/// Airport information with all available airport diagrams.
/// </summary>
public record AirportDiagramsResponseDto
{
    /// <summary>Official airport name.</summary>
    public string AirportName { get; init; } = string.Empty;
    /// <summary>ICAO identifier (e.g., KDFW).</summary>
    public string? IcaoIdent { get; init; }
    /// <summary>FAA airport identifier (e.g., DFW).</summary>
    public string? AirportIdent { get; init; }
    /// <summary>List of available airport diagrams with download URLs.</summary>
    public List<AirportDiagramDto> Diagrams { get; init; } = new();
}

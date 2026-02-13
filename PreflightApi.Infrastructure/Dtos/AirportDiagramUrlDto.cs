namespace PreflightApi.Infrastructure.Dtos;

/// <summary>
/// An airport diagram (e.g., Airport Diagram, Hot Spot, taxi chart) with a time-limited pre-signed URL for PDF download.
/// </summary>
public record AirportDiagramDto
{
    /// <summary>Name of the chart/diagram (e.g., "AIRPORT DIAGRAM", "HOT SPOT").</summary>
    public string ChartName { get; init; } = string.Empty;
    /// <summary>Pre-signed URL to download the diagram PDF. This URL expires after a limited time period; request a new URL if it has expired.</summary>
    public string PdfUrl { get; init; } = string.Empty;
}

/// <summary>
/// Airport information with all available airport diagrams. Diagrams are FAA-published PDF charts
/// showing taxiways, runways, and other ground features. The PDF URLs are time-limited pre-signed URLs.
/// </summary>
public record AirportDiagramsResponseDto
{
    /// <summary>Official airport name.</summary>
    public string AirportName { get; init; } = string.Empty;
    /// <summary>ICAO identifier (e.g., KDFW). Use this to cross-reference with other endpoints such as METARs and TAFs.</summary>
    public string? IcaoIdent { get; init; }
    /// <summary>FAA airport identifier (e.g., DFW). Use this to cross-reference with the Airports endpoint.</summary>
    public string? AirportIdent { get; init; }
    /// <summary>List of available airport diagram PDFs with time-limited download URLs.</summary>
    public List<AirportDiagramDto> Diagrams { get; init; } = new();
}

namespace PreflightApi.Infrastructure.Dtos;

/// <summary>
/// A terminal procedure chart (IAP, DP, STAR, APD, MIN, HOT, etc.) with a time-limited pre-signed URL for PDF download.
/// </summary>
public record TerminalProcedureDto
{
    /// <summary>Chart code indicating the procedure type (e.g., "IAP", "DP", "STAR", "APD", "MIN", "HOT").</summary>
    public string ChartCode { get; init; } = string.Empty;
    /// <summary>Name of the chart (e.g., "ILS OR LOC RWY 18L", "AIRPORT DIAGRAM").</summary>
    public string ChartName { get; init; } = string.Empty;
    /// <summary>Pre-signed URL to download the chart PDF. This URL expires after a limited time period; request a new URL if it has expired.</summary>
    public string PdfUrl { get; init; } = string.Empty;
    /// <summary>Amendment number, if applicable.</summary>
    public string? AmendmentNumber { get; init; }
    /// <summary>Amendment date, if applicable.</summary>
    public string? AmendmentDate { get; init; }
}

/// <summary>
/// Airport information with all available terminal procedure charts. Charts are FAA-published PDFs from the
/// Digital Terminal Procedures Publication (d-TPP). The PDF URLs are time-limited pre-signed URLs.
/// </summary>
public record TerminalProceduresResponseDto
{
    /// <summary>Official airport name.</summary>
    public string AirportName { get; init; } = string.Empty;
    /// <summary>ICAO identifier (e.g., KDFW). Use this to cross-reference with other endpoints such as METARs and TAFs.</summary>
    public string? IcaoIdent { get; init; }
    /// <summary>FAA airport identifier (e.g., DFW). Use this to cross-reference with the Airports endpoint.</summary>
    public string? AirportIdent { get; init; }
    /// <summary>List of available terminal procedure chart PDFs with time-limited download URLs.</summary>
    public List<TerminalProcedureDto> Procedures { get; init; } = new();
}

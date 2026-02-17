using PreflightApi.Infrastructure.Dtos.Notam;

namespace PreflightApi.Infrastructure.Dtos.Briefing;

/// <summary>
/// Composite weather briefing for a flight route, containing all weather products
/// affecting the route corridor.
/// </summary>
public record RouteBriefingResponse
{
    /// <summary>Route description (e.g., "KDFW -> KAUS").</summary>
    public string Route { get; init; } = string.Empty;

    /// <summary>Corridor width in nautical miles used for the search.</summary>
    public double CorridorWidthNm { get; init; }

    /// <summary>UTC timestamp when the briefing was generated.</summary>
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;

    /// <summary>METARs for airports found along the route corridor.</summary>
    public List<MetarDto> Metars { get; init; } = [];

    /// <summary>TAFs for airports found along the route corridor.</summary>
    public List<TafDto> Tafs { get; init; } = [];

    /// <summary>PIREPs within the route corridor.</summary>
    public List<PirepDto> Pireps { get; init; } = [];

    /// <summary>SIGMETs whose boundaries intersect the route.</summary>
    public List<SigmetDto> Sigmets { get; init; } = [];

    /// <summary>G-AIRMETs whose boundaries intersect the route.</summary>
    public List<GAirmetDto> GAirmets { get; init; } = [];

    /// <summary>Active NOTAMs within the route corridor.</summary>
    public List<NotamDto> Notams { get; init; } = [];

    /// <summary>Summary counts for each weather product category.</summary>
    public RouteBriefingSummary Summary { get; init; } = new();
}

/// <summary>
/// Count summary for a route briefing response.
/// </summary>
public record RouteBriefingSummary
{
    public int MetarCount { get; init; }
    public int TafCount { get; init; }
    public int PirepCount { get; init; }
    public int SigmetCount { get; init; }
    public int GAirmetCount { get; init; }
    public int NotamCount { get; init; }
}

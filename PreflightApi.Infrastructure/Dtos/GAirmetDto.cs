using PreflightApi.Domain.Enums;
using PreflightApi.Domain.ValueObjects.GAirmets;

namespace PreflightApi.Infrastructure.Dtos;

/// <summary>
/// G-AIRMET (Graphical AIRMET) advisory data with hazard, altitude, and area information.
/// </summary>
public record GAirmetDto
{
    /// <summary>Database identifier.</summary>
    public int Id { get; init; }
    /// <summary>Time the advisory was received.</summary>
    public DateTime ReceiptTime { get; init; }
    /// <summary>Time the advisory was issued.</summary>
    public DateTime IssueTime { get; init; }
    /// <summary>Time the advisory expires, typically 6 hours after issuance.</summary>
    public DateTime ExpireTime { get; init; }
    /// <summary>The valid time of the G-AIRMET snapshot.</summary>
    public DateTime ValidTime { get; init; }
    /// <summary>Product type: SIERRA, TANGO, or ZULU.</summary>
    public GAirmetProduct? Product { get; init; }
    /// <summary>Forecast component identifier tag. ex: 1C</summary>
    public string? Tag { get; init; }
    /// <summary>The forecast hour taken from initial product issuance. ex: 0, 3, 6, 9, 12</summary>
    public int ForecastHour { get; init; }
    /// <summary>Hazard type: IFR, MT_OBSC, TURB_HI, TURB_LO, ICE, FZLVL, M_FZLVL, SFC_WIND, or LLWS.</summary>
    public GAirmetHazardType? Hazard { get; init; }
    /// <summary>Hazard severity: MOD (moderate) or null.</summary>
    public HazardSeverity? HazardSeverity { get; init; }
    /// <summary>The geometry type: AREA or LINE.</summary>
    public string? GeometryType { get; init; }
    /// <summary>Additional information, reason for the AIRMET. ex: CIG BLW 010/VIS BLW 3SM PCPN/BR/FG</summary>
    public string? DueTo { get; init; }
    /// <summary>Altitude ranges for the advisory in feet MSL. 0 indicates surface, -1 indicates freezing level.</summary>
    public List<GAirmetAltitude>? Altitudes { get; init; }
    /// <summary>Geographic area affected by the advisory.</summary>
    public GAirmetArea? Area { get; init; }
}

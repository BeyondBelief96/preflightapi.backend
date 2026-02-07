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
    /// <summary>Time the advisory expires.</summary>
    public DateTime ExpireTime { get; init; }
    /// <summary>Time the advisory is valid for.</summary>
    public DateTime ValidTime { get; init; }
    /// <summary>Product type: SIERRA, TANGO, or ZULU.</summary>
    public GAirmetProduct Product { get; init; }
    /// <summary>Identifying tag for the advisory.</summary>
    public string? Tag { get; init; }
    /// <summary>Forecast hour offset.</summary>
    public int ForecastHour { get; init; }
    /// <summary>Hazard type (e.g., ICE, TURB_LO, IFR).</summary>
    public GAirmetHazardType? Hazard { get; init; }
    /// <summary>Hazard severity description.</summary>
    public string? HazardSeverity { get; init; }
    /// <summary>Geometry type of the affected area.</summary>
    public string? GeometryType { get; init; }
    /// <summary>Cause of the hazard.</summary>
    public string? DueTo { get; init; }
    /// <summary>Altitude ranges for the advisory.</summary>
    public List<GAirmetAltitude>? Altitudes { get; init; }
    /// <summary>Geographic area affected by the advisory.</summary>
    public GAirmetArea? Area { get; init; }
}

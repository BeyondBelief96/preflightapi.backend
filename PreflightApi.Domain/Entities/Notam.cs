using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace PreflightApi.Domain.Entities;

/// <summary>
/// A NOTAM (Notice to Air Missions) synced from the FAA NMS system.
/// Semi-normalized: indexed columns for common queries, plus the full GeoJSON Feature in <c>feature_json</c>.
/// Expired and cancelled NOTAMs are periodically purged by the background sync jobs.
/// </summary>
[Table("notams")]
public class Notam
{
    /// <summary>
    /// NMS identifier (primary key)
    /// </summary>
    [Key]
    [Column("nms_id", TypeName = "varchar(64)")]
    public string NmsId { get; set; } = string.Empty;

    /// <summary>
    /// Domestic location code (e.g., "CLT")
    /// </summary>
    [Column("location", TypeName = "varchar(20)")]
    public string? Location { get; set; }

    /// <summary>
    /// ICAO location code (e.g., "KCLT")
    /// </summary>
    [Column("icao_location", TypeName = "varchar(20)")]
    public string? IcaoLocation { get; set; }

    /// <summary>
    /// Classification: INTERNATIONAL, MILITARY, LOCAL_MILITARY, DOMESTIC, FDC
    /// </summary>
    [Column("classification", TypeName = "varchar(20)")]
    public string? Classification { get; set; }

    /// <summary>
    /// NOTAM type: N=New, R=Replace, C=Cancel
    /// </summary>
    [Column("notam_type", TypeName = "varchar(5)")]
    public string? NotamType { get; set; }

    /// <summary>
    /// Bare NOTAM sequence number (e.g., "420", "3997")
    /// </summary>
    [Column("notam_number", TypeName = "varchar(20)")]
    public string? NotamNumber { get; set; }

    /// <summary>
    /// 4-digit NOTAM year (e.g., "2025")
    /// </summary>
    [Column("notam_year", TypeName = "varchar(4)")]
    public string? NotamYear { get; set; }

    /// <summary>
    /// ICAO series letter (e.g., "A", "B", "C")
    /// </summary>
    [MaxLength(10)]
    [Column("series", TypeName = "varchar(10)")]
    public string? Series { get; set; }

    /// <summary>
    /// Accountability code (e.g., "BNA", "FDC")
    /// </summary>
    [Column("account_id", TypeName = "varchar(20)")]
    public string? AccountId { get; set; }

    /// <summary>
    /// Airport/facility name from AIXM (null for GeoJSON-sourced NOTAMs)
    /// </summary>
    [Column("airport_name", TypeName = "varchar(100)")]
    public string? AirportName { get; set; }

    /// <summary>
    /// Effective start date
    /// </summary>
    [Column("effective_start")]
    public DateTime? EffectiveStart { get; set; }

    /// <summary>
    /// Effective end date (null for permanent NOTAMs where effectiveEnd is "PERM")
    /// </summary>
    [Column("effective_end")]
    public DateTime? EffectiveEnd { get; set; }

    /// <summary>
    /// Cancellation date — non-null means this NOTAM has been cancelled
    /// </summary>
    [Column("cancelation_date")]
    public DateTime? CancelationDate { get; set; }

    /// <summary>
    /// NOTAM text content (for free-text search)
    /// </summary>
    [Column("text")]
    public string? Text { get; set; }

    /// <summary>
    /// When NMS last updated this NOTAM
    /// </summary>
    [Column("last_updated")]
    public DateTime? LastUpdated { get; set; }

    /// <summary>
    /// When our system last synced this record
    /// </summary>
    [Column("synced_at")]
    public DateTime SyncedAt { get; set; }

    /// <summary>
    /// Full GeoJSON Feature serialized as JSON (stored as jsonb)
    /// </summary>
    [Column("feature_json", TypeName = "jsonb")]
    public string FeatureJson { get; set; } = string.Empty;

    /// <summary>
    /// PostGIS geometry parsed from GeoJSON coordinates
    /// </summary>
    [Column("geometry", TypeName = "geometry(Geometry, 4326)")]
    public Geometry? Geometry { get; set; }
}

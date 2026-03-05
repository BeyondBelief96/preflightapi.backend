using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace PreflightApi.Domain.Entities;

/// <summary>
/// Obstacle data from the FAA Digital Obstacle File (DOF).
/// Contains towers, buildings, and other obstructions to air navigation.
/// </summary>
[Table("obstacles")]
public class Obstacle
{
    /// <summary>OAS (Obstacle Accountability System) unique number. Primary key.</summary>
    [Key]
    [Column("oas_number")]
    public string OasNumber { get; set; } = string.Empty;

    /// <summary>OAS code identifying the obstacle data source.</summary>
    [Column("oas_code")]
    public string OasCode { get; set; } = string.Empty;

    /// <summary>Obstacle number within the OAS code group.</summary>
    [Column("obstacle_number")]
    public string ObstacleNumber { get; set; } = string.Empty;

    /// <summary>Verification status of the obstacle data.</summary>
    [Column("verification_status")]
    public string? VerificationStatus { get; set; }

    /// <summary>Country identifier (e.g., "US").</summary>
    [Column("country_id")]
    public string? CountryId { get; set; }

    /// <summary>Two-letter state identifier.</summary>
    [Column("state_id")]
    public string? StateId { get; set; }

    /// <summary>City name nearest the obstacle.</summary>
    [Column("city_name")]
    public string? CityName { get; set; }

    /// <summary>Latitude degrees component of the DMS coordinate.</summary>
    [Column("lat_degrees")]
    public int? LatDegrees { get; set; }

    /// <summary>Latitude minutes component of the DMS coordinate.</summary>
    [Column("lat_minutes")]
    public int? LatMinutes { get; set; }

    /// <summary>Latitude seconds component of the DMS coordinate.</summary>
    [Column("lat_seconds", TypeName = "decimal(6,2)")]
    public decimal? LatSeconds { get; set; }

    /// <summary>Latitude hemisphere (N or S).</summary>
    [Column("lat_hemisphere")]
    public string? LatHemisphere { get; set; }

    /// <summary>Longitude degrees component of the DMS coordinate.</summary>
    [Column("long_degrees")]
    public int? LongDegrees { get; set; }

    /// <summary>Longitude minutes component of the DMS coordinate.</summary>
    [Column("long_minutes")]
    public int? LongMinutes { get; set; }

    /// <summary>Longitude seconds component of the DMS coordinate.</summary>
    [Column("long_seconds", TypeName = "decimal(6,2)")]
    public decimal? LongSeconds { get; set; }

    /// <summary>Longitude hemisphere (E or W).</summary>
    [Column("long_hemisphere")]
    public string? LongHemisphere { get; set; }

    /// <summary>Latitude in decimal degrees (WGS 84).</summary>
    [Column("lat_decimal", TypeName = "decimal(10,8)")]
    public decimal? LatDecimal { get; set; }

    /// <summary>Longitude in decimal degrees (WGS 84).</summary>
    [Column("long_decimal", TypeName = "decimal(11,8)")]
    public decimal? LongDecimal { get; set; }

    /// <summary>Type of obstacle (e.g., TOWER, BLDG, STACK, POLE).</summary>
    [Column("obstacle_type")]
    public string? ObstacleType { get; set; }

    /// <summary>Number of obstacles at this location.</summary>
    [Column("quantity")]
    public int? Quantity { get; set; }

    /// <summary>Height of the obstacle in feet AGL.</summary>
    [Column("height_agl")]
    public int? HeightAgl { get; set; }

    /// <summary>Height of the obstacle in feet AMSL.</summary>
    [Column("height_amsl")]
    public int? HeightAmsl { get; set; }

    /// <summary>Obstacle lighting type (e.g., R=Red, D=Dual, F=Flashing, S=Strobe, N=None).</summary>
    [Column("lighting")]
    public string? Lighting { get; set; }

    /// <summary>Horizontal accuracy code for the obstacle position.</summary>
    [Column("horizontal_accuracy")]
    public string? HorizontalAccuracy { get; set; }

    /// <summary>Vertical accuracy code for the obstacle height.</summary>
    [Column("vertical_accuracy")]
    public string? VerticalAccuracy { get; set; }

    /// <summary>Mark indicator (e.g., P=Painted, F=Flagged, N=None).</summary>
    [Column("mark_indicator")]
    public string? MarkIndicator { get; set; }

    /// <summary>FAA aeronautical study number associated with the obstacle.</summary>
    [Column("faa_study_number")]
    public string? FaaStudyNumber { get; set; }

    /// <summary>Action code indicating the record's status (e.g., A=Add, C=Change, D=Dismantle).</summary>
    [Column("action")]
    public string? Action { get; set; }

    /// <summary>Julian date of the last action on this record.</summary>
    [Column("julian_date")]
    public string? JulianDate { get; set; }

    /// <summary>PostGIS geography point computed from LatDecimal/LongDecimal via database trigger.</summary>
    [Column("location", TypeName = "geography(Point, 4326)")]
    public Point? Location { get; set; }
}

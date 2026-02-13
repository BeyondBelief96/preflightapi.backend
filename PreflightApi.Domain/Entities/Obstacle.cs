using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace PreflightApi.Domain.Entities;

[Table("obstacles")]
public class Obstacle
{
    [Key]
    [Column("oas_number")]
    public string OasNumber { get; set; } = string.Empty;

    [Column("oas_code")]
    public string OasCode { get; set; } = string.Empty;

    [Column("obstacle_number")]
    public string ObstacleNumber { get; set; } = string.Empty;

    [Column("verification_status")]
    public string? VerificationStatus { get; set; }

    [Column("country_id")]
    public string? CountryId { get; set; }

    [Column("state_id")]
    public string? StateId { get; set; }

    [Column("city_name")]
    public string? CityName { get; set; }

    [Column("lat_degrees")]
    public int? LatDegrees { get; set; }

    [Column("lat_minutes")]
    public int? LatMinutes { get; set; }

    [Column("lat_seconds", TypeName = "decimal(6,2)")]
    public decimal? LatSeconds { get; set; }

    [Column("lat_hemisphere")]
    public string? LatHemisphere { get; set; }

    [Column("long_degrees")]
    public int? LongDegrees { get; set; }

    [Column("long_minutes")]
    public int? LongMinutes { get; set; }

    [Column("long_seconds", TypeName = "decimal(6,2)")]
    public decimal? LongSeconds { get; set; }

    [Column("long_hemisphere")]
    public string? LongHemisphere { get; set; }

    [Column("lat_decimal", TypeName = "decimal(10,8)")]
    public decimal? LatDecimal { get; set; }

    [Column("long_decimal", TypeName = "decimal(11,8)")]
    public decimal? LongDecimal { get; set; }

    [Column("obstacle_type")]
    public string? ObstacleType { get; set; }

    [Column("quantity")]
    public int? Quantity { get; set; }

    [Column("height_agl")]
    public int? HeightAgl { get; set; }

    [Column("height_amsl")]
    public int? HeightAmsl { get; set; }

    [Column("lighting")]
    public string? Lighting { get; set; }

    [Column("horizontal_accuracy")]
    public string? HorizontalAccuracy { get; set; }

    [Column("vertical_accuracy")]
    public string? VerticalAccuracy { get; set; }

    [Column("mark_indicator")]
    public string? MarkIndicator { get; set; }

    [Column("faa_study_number")]
    public string? FaaStudyNumber { get; set; }

    [Column("action")]
    public string? Action { get; set; }

    [Column("julian_date")]
    public string? JulianDate { get; set; }

    [Column("location", TypeName = "geography(Point, 4326)")]
    public Point? Location { get; set; }
}

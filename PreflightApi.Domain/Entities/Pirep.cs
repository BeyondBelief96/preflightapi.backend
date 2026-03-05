using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using NetTopologySuite.Geometries;
using PreflightApi.Domain.ValueObjects.Pireps;

namespace PreflightApi.Domain.Entities
{
    /// <summary>
    /// PIREP (Pilot Report) data from the NOAA Aviation Weather Center.
    /// Sourced from the AvWx cache XML feed (pirep1_0.xsd schema).
    /// </summary>
    [Table("pirep")]
    public class Pirep
    {
        /// <summary>
        /// Database-generated unique identifier for the PIREP.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// The time the observation was received (ISO 8601 date format)
        /// </summary>
        [Column("receipt_time")]
        public string? ReceiptTime { get; set; }

        /// <summary>
        /// The observation time (ISO 8601 date format)
        /// </summary>
        [Column("observation_time")]
        public string? ObservationTime { get; set; }

        /// <summary>
        /// Quality control flags indicating potential data issues with the report (e.g., assumed midpoint, missing timestamp, bad location).
        /// </summary>
        [Column("quality_control_flags", TypeName = "jsonb")]
        public PirepQualityControlFlags? QualityControlFlags { get; set; }

        /// <summary>
        /// Aircraft type designation. XSD field: ac_type. ex: B738, C172
        /// </summary>
        [Column("aircraft_ref")]
        public string? AircraftRef { get; set; }

        /// <summary>
        /// Latitude of site in degrees ex: 41.9602
        /// </summary>
        [Column("latitude")]
        public double? Latitude { get; set; }

        /// <summary>
        /// Longitude of site in degrees ex: -87.9316
        /// </summary>
        [Column("longitude")]
        public double? Longitude { get; set; }

        /// <summary>
        /// Altitude in feet MSL ex: 10000
        /// </summary>
        [Column("altitude_ft_msl")]
        public int? AltitudeFtMsl { get; set; }
        
        /// <summary>
        /// Reported sky conditions (cloud cover, base, and top altitudes in feet MSL).
        /// </summary>
        [Column("sky_condition", TypeName = "jsonb")]
        public List<PirepSkyCondition>? SkyConditions { get; set; }

        /// <summary>
        /// Reported turbulence conditions (type, intensity, base/top altitudes, and frequency).
        /// </summary>
        [Column("turbulence_condition", TypeName = "jsonb")]
        public List<PirepTurbulenceCondition>? TurbulenceConditions { get; set; }

        /// <summary>
        /// Reported icing conditions (type, intensity, and base/top altitudes).
        /// </summary>
        [Column("icing_condition", TypeName = "jsonb")]
        public List<PirepIcingCondition>? IcingConditions { get; set; }

        /// <summary>
        /// Flight visibility in statute miles.
        /// </summary>
        [Column("visibility_statute_mi")]
        public int? VisibilityStatuteMi { get; set; }

        /// <summary>
        /// Present weather string. Encoded weather phenomena observed by the pilot.
        /// </summary>
        [Column("wx_string")]
        public string? WxString { get; set; }

        /// <summary>
        /// Temperature in Celsius ex: 15
        /// </summary>
        [Column("temp_c")]
        public double? TempC { get; set; }

        /// <summary>
        /// Wind direction in degrees ex: 180
        /// </summary>
        [Column("wind_dir_degrees")]
        public int? WindDirDegrees { get; set; }

        /// <summary>
        /// Wind speed in knots ex: 10
        /// </summary>
        [Column("wind_speed_kt")]
        public int? WindSpeedKt { get; set; }

        /// <summary>
        /// Vertical gust speed in knots ex: 10
        /// </summary>
        [Column("vert_gust_kt")]
        public int? VertGustKt { get; set; }

        /// <summary>
        /// Report type: UA (routine PIREP), UUA (urgent PIREP). XSD field: pirep_type.
        /// </summary>
        [Column("report_type")]
        public string? ReportType { get; set; }

        /// <summary>
        /// Raw text of observation
        /// </summary>
        [Column("raw_text")]
        public string? RawText { get; set; }

        /// <summary>
        /// PostGIS geography point computed from Latitude/Longitude via database trigger.
        /// </summary>
        [Column("location", TypeName = "geography(Point, 4326)")]
        public Point? Location { get; set; }
    }
}

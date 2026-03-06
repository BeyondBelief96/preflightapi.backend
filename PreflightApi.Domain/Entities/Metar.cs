using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PreflightApi.Domain.ValueObjects.Metar;

namespace PreflightApi.Domain.Entities;

/// <summary>
/// METAR (Meteorological Aerodrome Report) observation data from the NOAA Aviation Weather Center.
/// Sourced from the AvWx cache XML feed (metar1_3.xsd schema).
/// </summary>
[Table("metar")]
public class Metar
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Raw text of observation ex: KORD 032151Z 23006KT 10SM BKN110 OVC250 14/03 A3000 RMK AO2 SLP162 VIRGA OHD T01440028
    /// </summary>
    [Column("raw_text")]
    public string? RawText { get; set; }

    /// <summary>
    /// ICAO identifier ex: KORD
    /// </summary>
    [Column("station_id")]
    public string? StationId { get; set; }

    /// <summary>
    /// The observation time. ISO 8601 UTC format. ex: 2023-11-06T20:51:00Z
    /// </summary>
    [Column("observation_time")]
    public string? ObservationTime { get; set; }

    /// <summary>
    /// Latitude of site in decimal degrees (WGS 84). ex: 41.9602
    /// </summary>
    [Column("latitude")]
    public double? Latitude { get; set; }

    /// <summary>
    /// Longitude of site in decimal degrees (WGS 84). ex: -87.9316
    /// </summary>
    [Column("longitude")]
    public double? Longitude { get; set; }

    /// <summary>
    /// Temperature in degrees Celsius ex: 14.4
    /// </summary>
    [Column("temp_c")]
    public double? TempC { get; set; }

    /// <summary>
    /// Dewpoint temperature in degrees Celsius ex: 2.8
    /// </summary>
    [Column("dewpoint_c")]
    public double? DewpointC { get; set; }

    /// <summary>
    /// Wind direction in degrees true, or VRB for variable winds. ex: 230, VRB
    /// </summary>
    [Column("wind_dir_degrees")]
    public string? WindDirDegrees { get; set; }

    /// <summary>
    /// Wind speed in knots.
    /// </summary>
    [Column("wind_speed_kt")]
    public int? WindSpeedKt { get; set; }

    /// <summary>
    /// Wind gust speed in knots, if available.
    /// </summary>
    [Column("wind_gust_kt")]
    public int? WindGustKt { get; set; }

    /// <summary>
    /// Visibility in statute miles, 10+ is greater than 10 sm ex: 3, 10+
    /// </summary>
    [Column("visibility_statute_mi")]
    public string? VisibilityStatuteMi { get; set; }

    /// <summary>
    /// Altimeter setting in inches of mercury ex: 29.92
    /// </summary>
    [Column("altim_in_hg")]
    public double? AltimInHg { get; set; }

    /// <summary>
    /// Sea level pressure in millibars. ex: 1013.25
    /// </summary>
    [Column("sea_level_pressure_mb")]
    public double? SeaLevelPressureMb { get; set; }

    /// <summary>
    /// Quality control flags associated with the METAR data.
    /// </summary>
    [Column("quality_control_flags", TypeName = "jsonb")]
    public MetarQualityControlFlags? QualityControlFlags { get; set; }

    /// <summary>
    /// Present weather string. Encoded weather phenomena (e.g., -RA for light rain, +TSRA for heavy thunderstorms with rain, FG for fog).
    /// </summary>
    [Column("wx_string")]
    public string? WxString { get; set; }

    /// <summary>
    /// Maximum of 4 sky conditions as per XSD schema
    /// </summary>
    [Column("sky_condition", TypeName = "jsonb")]
    public List<MetarSkyCondition>? SkyCondition { get; set; }

    /// <summary>
    /// Flight category, if available. ex: VFR, MVFR, IFR, LIFR
    /// </summary>
    [Column("flight_category")]
    public string? FlightCategory { get; set; }

    /// <summary>
    /// Three-hour pressure tendency in millibars.
    /// </summary>
    [Column("three_hr_pressure_tendency_mb")]
    public double? ThreeHrPressureTendencyMb { get; set; }

    /// <summary>
    /// Maximum temperature in degrees Celsius, if available. ex: 15.0
    /// </summary>
    [Column("maxT_c")]
    public double? MaxTC { get; set; }

    /// <summary>
    /// Minimum temperature in degrees Celsius, if available. ex: 3.0
    /// </summary>
    [Column("minT_c")]
    public double? MinTC { get; set; }

    /// <summary>
    /// Maximum temperature in degrees Celsius over the past 24 hours, if available. ex: 15.0
    /// </summary>
    [Column("maxT24hr_c")]
    public double? MaxT24hrC { get; set; }

    /// <summary>
    /// Minimum temperature in degrees Celsius over the past 24 hours, if available. ex: 3.0
    /// </summary>
    [Column("minT24hr_c")]
    public double? MinT24hrC { get; set; }

    /// <summary>
    /// Precipitation accumulation in inches since the last routine observation. ex: 0.0
    /// </summary>
    [Column("precip_in")]
    public double? PrecipIn { get; set; }

    /// <summary>
    /// Precipitation accumulation in inches over the past 3 hours, if available. ex: 0.0
    /// </summary>
    [Column("pcp3hr_in")]
    public double? Pcp3hrIn { get; set; }

    /// <summary>
    /// Precipitation accumulation in inches over the past 6 hours, if available. ex: 0.0
    /// </summary>
    [Column("pcp6hr_in")]
    public double? Pcp6hrIn { get; set; }

    /// <summary>
    /// Precipitation accumulation in inches over the past 24 hours, if available. ex: 0.0
    /// </summary>
    [Column("pcp24hr_in")]
    public double? Pcp24hrIn { get; set; }

    /// <summary>
    /// Snow depth in inches, if available. ex: 0.0
    /// </summary>
    [Column("snow_in")]
    public double? SnowIn { get; set; }

    /// <summary>
    /// Vertical visibility in feet AGL, if available. ex: 1000
    /// </summary>
    [Column("vert_vis_ft")]
    public int? VertVisFt { get; set; }

    /// <summary>
    /// Type of encoding ex: METAR, SPECI, SYNOP, BUOY, CMAN
    /// </summary>
    [Column("metar_type")]
    public string? MetarType { get; set; }

    /// <summary>
    /// Elevation of site in meters MSL. This field is in meters (not feet) because it originates from the NOAA Aviation Weather API which uses ICAO standard units. ex: 202
    /// </summary>
    [Column("elevation_m")]
    public double? ElevationM { get; set; }
}
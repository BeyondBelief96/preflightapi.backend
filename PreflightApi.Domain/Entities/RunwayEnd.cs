using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PreflightApi.Domain.Entities;

/// <summary>
/// Runway end data from the FAA NASR database. Sourced from APT_RWY_END CSV file in the FAA NASR 28-day subscription.
/// Each runway typically has two runway ends (one for each direction). Ordered by SITE_NO, SITE_TYPE_CODE, RWY_ID, RWY_END_ID.
/// </summary>
[Table("runway_ends")]
public class RunwayEnd : INasrEntity<RunwayEnd>
{
    /// <summary>System-generated unique identifier.</summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    /// <summary>FAA NASR field: SITE_NO. Unique Site Number of the parent airport facility.</summary>
    [Column("site_no")]
    [Required]
    public string SiteNo { get; set; } = string.Empty;

    /// <summary>FAA NASR field: RWY_ID. Runway identification of the parent runway.</summary>
    [Column("runway_id_ref")]
    [Required]
    public string RunwayIdRef { get; set; } = string.Empty;

    /// <summary>Foreign key to the parent Runway entity. Populated after data sync by linking on SiteNo and RunwayIdRef.</summary>
    [Column("runway_fk")]
    public Guid? RunwayFk { get; set; }

    /// <summary>FAA NASR field: RWY_END_ID. Runway end identifier (e.g., "01", "19", "09L", "27R").</summary>
    [Column("runway_end_id")]
    [Required]
    public string RunwayEndId { get; set; } = string.Empty;

    /// <summary>FAA NASR field: TRUE_ALIGNMENT. Runway end true alignment. True heading of the runway to the nearest degree.</summary>
    [Column("true_alignment")]
    public int? TrueAlignment { get; set; }

    /// <summary>
    /// FAA NASR field: ILS_TYPE. Instrument Landing System (ILS) type.
    /// <para>Possible values: ILS, MLS, SDF, LOCALIZER, LDA, ISMLS, ILS/DME, SDF/DME, LOC/DME, LOC/GS, LDA/DME.</para>
    /// </summary>
    [Column("approach_type")]
    public string? ApproachType { get; set; }

    /// <summary>
    /// FAA NASR field: RIGHT_HAND_TRAFFIC_PAT_FLAG. Whether right-hand traffic pattern is in effect for landing aircraft.
    /// <para>Possible values: Y (Yes), N (No).</para>
    /// </summary>
    [Column("right_hand_traffic_pattern")]
    public bool RightHandTrafficPattern { get; set; }

    /// <summary>
    /// FAA NASR field: RWY_MARKING_TYPE_CODE. Runway markings type.
    /// <para>Possible values: PIR (Precision Instrument), NPI (Nonprecision Instrument), BSC (Basic),
    /// NRS (Numbers Only), NSTD (Nonstandard), BUOY (Buoys - Seaplane Base), STOL (Short Takeoff and Landing), NONE.</para>
    /// </summary>
    [Column("runway_markings_type")]
    public string? RunwayMarkingsType { get; set; }

    /// <summary>
    /// FAA NASR field: RWY_MARKING_COND. Runway markings condition.
    /// <para>Possible values: G (Good), F (Fair), P (Poor).</para>
    /// </summary>
    [Column("runway_markings_condition")]
    public string? RunwayMarkingsCondition { get; set; }

    /// <summary>FAA NASR field: LAT_DECIMAL. Latitude of physical runway end in decimal degrees.</summary>
    [Column("lat_decimal", TypeName = "decimal(10,8)")]
    public decimal? LatDecimal { get; set; }

    /// <summary>FAA NASR field: LONG_DECIMAL. Longitude of physical runway end in decimal degrees.</summary>
    [Column("long_decimal", TypeName = "decimal(11,8)")]
    public decimal? LongDecimal { get; set; }

    /// <summary>FAA NASR field: RWY_END_ELEV. Elevation at the physical runway end in feet MSL.</summary>
    [Column("elevation", TypeName = "decimal(7,1)")]
    public decimal? Elevation { get; set; }

    /// <summary>FAA NASR field: THR_CROSSING_HGT. Threshold Crossing Height in feet AGL. Height that the effective visual glide path crosses above the runway threshold.</summary>
    [Column("threshold_crossing_height", TypeName = "decimal(5,1)")]
    public decimal? ThresholdCrossingHeight { get; set; }

    /// <summary>FAA NASR field: VISUAL_GLIDE_PATH_ANGLE. Visual glide path angle in hundredths of degrees.</summary>
    [Column("visual_glide_path_angle", TypeName = "decimal(4,2)")]
    public decimal? VisualGlidePathAngle { get; set; }

    /// <summary>FAA NASR field: LAT_DISPLACED_THR_DECIMAL. Latitude of displaced threshold in decimal degrees.</summary>
    [Column("displaced_threshold_lat_decimal", TypeName = "decimal(10,8)")]
    public decimal? DisplacedThresholdLatDecimal { get; set; }

    /// <summary>FAA NASR field: LONG_DISPLACED_THR_DECIMAL. Longitude of displaced threshold in decimal degrees.</summary>
    [Column("displaced_threshold_long_decimal", TypeName = "decimal(11,8)")]
    public decimal? DisplacedThresholdLongDecimal { get; set; }

    /// <summary>FAA NASR field: DISPLACED_THR_ELEV. Elevation at the displaced threshold in feet MSL.</summary>
    [Column("displaced_threshold_elev", TypeName = "decimal(7,1)")]
    public decimal? DisplacedThresholdElev { get; set; }

    /// <summary>FAA NASR field: DISPLACED_THR_LEN. Displaced threshold length in feet from the runway end.</summary>
    [Column("displaced_threshold_length")]
    public int? DisplacedThresholdLength { get; set; }

    /// <summary>FAA NASR field: TDZ_ELEV. Elevation at the touchdown zone in feet MSL.</summary>
    [Column("touchdown_zone_elev", TypeName = "decimal(7,1)")]
    public decimal? TouchdownZoneElev { get; set; }

    /// <summary>
    /// FAA NASR field: VGSI_CODE. Visual Glide Slope Indicator type.
    /// <para>Common values: S2L/S2R (SAVASI), V2L/V2R/V4L/V4R/V6L/V6R/V12/V16 (VASI),
    /// P2L/P2R/P4L/P4R (PAPI), TRIL/TRIR (Tri-Color), PSIL/PSIR (Pulsating),
    /// PNIL/PNIR (Panel), NSTD (Nonstandard), PVT (Private Use), VAS (Non-Specific), NONE/N.</para>
    /// </summary>
    [Column("visual_glide_slope_indicator")]
    public string? VisualGlideSlopeIndicator { get; set; }

    /// <summary>
    /// FAA NASR field: RWY_VISUAL_RANGE_EQUIP_CODE. Runway Visual Range (RVR) equipment location.
    /// Indicates location(s) at which RVR equipment is installed.
    /// <para>Possible values: T (Touchdown), M (Midfield), R (Rollout), N (No RVR Available),
    /// TM, TR, MR, TMR (combinations).</para>
    /// </summary>
    [Column("runway_visual_range_equipment")]
    public string? RunwayVisualRangeEquipment { get; set; }

    /// <summary>
    /// FAA NASR field: RWY_VSBY_VALUE_EQUIP_FLAG. Runway Visibility Value (RVV) equipment presence.
    /// <para>Possible values: Y (Yes), N (No).</para>
    /// </summary>
    [Column("runway_visibility_value_equipment")]
    public bool RunwayVisibilityValueEquipment { get; set; }

    /// <summary>
    /// FAA NASR field: APCH_LGT_SYSTEM_CODE. Approach light system type.
    /// <para>Possible values: AFOVRN, ALSAF, ALSF1, ALSF2, MALS, MALSF, MALSR, RAIL, SALS, SALSF,
    /// SSALS, SSALF, SSALR, ODALS, RLLS, MIL OVRN, NSTD, NONE.</para>
    /// </summary>
    [Column("approach_light_system")]
    public string? ApproachLightSystem { get; set; }

    /// <summary>
    /// FAA NASR field: RWY_END_LGTS_FLAG. Runway End Identifier Lights (REIL) availability.
    /// <para>Possible values: Y (Yes), N (No).</para>
    /// </summary>
    [Column("runway_end_lights")]
    public bool RunwayEndLights { get; set; }

    /// <summary>
    /// FAA NASR field: CNTRLN_LGTS_AVBL_FLAG. Runway centerline lights availability.
    /// <para>Possible values: Y (Yes), N (No).</para>
    /// </summary>
    [Column("centerline_lights")]
    public bool CenterlineLights { get; set; }

    /// <summary>
    /// FAA NASR field: TDZ_LGT_AVBL_FLAG. Runway end touchdown zone lights availability.
    /// <para>Possible values: Y (Yes), N (No).</para>
    /// </summary>
    [Column("touchdown_zone_lights")]
    public bool TouchdownZoneLights { get; set; }

    /// <summary>FAA NASR field: OBSTN_TYPE. Controlling object description (type of obstacle).</summary>
    [Column("controlling_object_description")]
    public string? ControllingObjectDescription { get; set; }

    /// <summary>
    /// FAA NASR field: OBSTN_MRKD_CODE. Controlling object marked/lighted status.
    /// <para>Possible values: M (Marked), L (Lighted), ML (Marked and Lighted), NONE.</para>
    /// </summary>
    [Column("controlling_object_marked_lighted")]
    public string? ControllingObjectMarkedLighted { get; set; }

    /// <summary>FAA NASR field: OBSTN_CLNC_SLOPE. Controlling object clearance slope value, expressed as a ratio of N:1. If greater than 50:1, then 50 is entered.</summary>
    [Column("controlling_object_clearance_slope")]
    public int? ControllingObjectClearanceSlope { get; set; }

    /// <summary>FAA NASR field: OBSTN_HGT. Controlling object height above the physical runway end in feet AGL.</summary>
    [Column("controlling_object_height_above_runway")]
    public int? ControllingObjectHeightAboveRunway { get; set; }

    /// <summary>FAA NASR field: DIST_FROM_THR. Controlling object distance from the physical runway end in feet. Measured using the extended runway centerline to a point abeam the object.</summary>
    [Column("controlling_object_distance_from_runway")]
    public int? ControllingObjectDistanceFromRunway { get; set; }

    /// <summary>FAA NASR field: CNTRLN_OFFSET. Controlling object centerline offset distance in feet. Distance that the controlling object is located away from the extended runway centerline, measured horizontally on a line perpendicular to the extended runway centerline.</summary>
    [Column("controlling_object_centerline_offset")]
    public string? ControllingObjectCenterlineOffset { get; set; }

    // DMS Coordinates - Runway End
    /// <summary>FAA NASR field: RWY_END_LAT_DEG. Runway end latitude degrees.</summary>
    [Column("rwy_end_lat_deg")]
    public int? RwyEndLatDeg { get; set; }

    /// <summary>FAA NASR field: RWY_END_LAT_MIN. Runway end latitude minutes.</summary>
    [Column("rwy_end_lat_min")]
    public int? RwyEndLatMin { get; set; }

    /// <summary>FAA NASR field: RWY_END_LAT_SEC. Runway end latitude seconds.</summary>
    [Column("rwy_end_lat_sec", TypeName = "decimal(6,2)")]
    public decimal? RwyEndLatSec { get; set; }

    /// <summary>FAA NASR field: RWY_END_LAT_HEMIS. Runway end latitude hemisphere.</summary>
    [Column("rwy_end_lat_hemis")]
    public string? RwyEndLatHemis { get; set; }

    /// <summary>FAA NASR field: RWY_END_LONG_DEG. Runway end longitude degrees.</summary>
    [Column("rwy_end_long_deg")]
    public int? RwyEndLongDeg { get; set; }

    /// <summary>FAA NASR field: RWY_END_LONG_MIN. Runway end longitude minutes.</summary>
    [Column("rwy_end_long_min")]
    public int? RwyEndLongMin { get; set; }

    /// <summary>FAA NASR field: RWY_END_LONG_SEC. Runway end longitude seconds.</summary>
    [Column("rwy_end_long_sec", TypeName = "decimal(6,2)")]
    public decimal? RwyEndLongSec { get; set; }

    /// <summary>FAA NASR field: RWY_END_LONG_HEMIS. Runway end longitude hemisphere.</summary>
    [Column("rwy_end_long_hemis")]
    public string? RwyEndLongHemis { get; set; }

    // DMS Coordinates - Displaced Threshold
    /// <summary>FAA NASR field: DISPLACED_THR_LAT_DEG. Displaced threshold latitude degrees.</summary>
    [Column("displaced_thr_lat_deg")]
    public int? DisplacedThrLatDeg { get; set; }

    /// <summary>FAA NASR field: DISPLACED_THR_LAT_MIN. Displaced threshold latitude minutes.</summary>
    [Column("displaced_thr_lat_min")]
    public int? DisplacedThrLatMin { get; set; }

    /// <summary>FAA NASR field: DISPLACED_THR_LAT_SEC. Displaced threshold latitude seconds.</summary>
    [Column("displaced_thr_lat_sec", TypeName = "decimal(6,2)")]
    public decimal? DisplacedThrLatSec { get; set; }

    /// <summary>FAA NASR field: DISPLACED_THR_LAT_HEMIS. Displaced threshold latitude hemisphere.</summary>
    [Column("displaced_thr_lat_hemis")]
    public string? DisplacedThrLatHemis { get; set; }

    /// <summary>FAA NASR field: DISPLACED_THR_LONG_DEG. Displaced threshold longitude degrees.</summary>
    [Column("displaced_thr_long_deg")]
    public int? DisplacedThrLongDeg { get; set; }

    /// <summary>FAA NASR field: DISPLACED_THR_LONG_MIN. Displaced threshold longitude minutes.</summary>
    [Column("displaced_thr_long_min")]
    public int? DisplacedThrLongMin { get; set; }

    /// <summary>FAA NASR field: DISPLACED_THR_LONG_SEC. Displaced threshold longitude seconds.</summary>
    [Column("displaced_thr_long_sec", TypeName = "decimal(6,2)")]
    public decimal? DisplacedThrLongSec { get; set; }

    /// <summary>FAA NASR field: DISPLACED_THR_LONG_HEMIS. Displaced threshold longitude hemisphere.</summary>
    [Column("displaced_thr_long_hemis")]
    public string? DisplacedThrLongHemis { get; set; }

    // Codes & Gradient
    /// <summary>
    /// FAA NASR field: FAR_PART_77_CODE. FAA CFR Part 77 (Objects Affecting Navigable Airspace) Runway Category.
    /// <para>Possible values: A(V) (Utility Runway with Visual Approach), B(V) (Other Than Utility with Visual Approach),
    /// A(NP) (Utility with Nonprecision Approach), C (Other Than Utility with Nonprecision, visibility &gt; 3/4 mile),
    /// D (Other Than Utility with Nonprecision, visibility as low as 3/4 mile), PIR (Precision Instrument Runway).</para>
    /// </summary>
    [Column("far_part_77_code")]
    public string? FarPart77Code { get; set; }

    /// <summary>FAA NASR field: CNTRLN_DIR_CODE. Controlling Object Centerline Offset Direction. Indicates direction (left or right) to the object from the centerline as seen by an approaching pilot.</summary>
    [Column("centerline_direction_code")]
    public string? CenterlineDirectionCode { get; set; }

    /// <summary>FAA NASR field: RWY_GRAD. Runway End Gradient.</summary>
    [Column("runway_gradient", TypeName = "decimal(5,2)")]
    public decimal? RunwayGradient { get; set; }

    /// <summary>FAA NASR field: RWY_GRAD_DIRECTION. Runway End Gradient Direction (Up or Down).</summary>
    [Column("runway_gradient_direction")]
    public string? RunwayGradientDirection { get; set; }

    // Source/Date Metadata
    /// <summary>FAA NASR field: RWY_END_PSN_SOURCE. Source of runway end position information.</summary>
    [Column("rwy_end_position_source")]
    public string? RwyEndPositionSource { get; set; }

    /// <summary>FAA NASR field: RWY_END_PSN_DATE. Date of runway end position information.</summary>
    [Column("rwy_end_position_date")]
    public DateTime? RwyEndPositionDate { get; set; }

    /// <summary>FAA NASR field: RWY_END_ELEV_SOURCE. Source of runway end elevation information.</summary>
    [Column("rwy_end_elevation_source")]
    public string? RwyEndElevationSource { get; set; }

    /// <summary>FAA NASR field: RWY_END_ELEV_DATE. Date of runway end elevation information.</summary>
    [Column("rwy_end_elevation_date")]
    public DateTime? RwyEndElevationDate { get; set; }

    /// <summary>FAA NASR field: DSPL_THR_PSN_SOURCE. Source of displaced threshold position information.</summary>
    [Column("displaced_thr_position_source")]
    public string? DisplacedThrPositionSource { get; set; }

    /// <summary>FAA NASR field: RWY_END_DSPL_THR_PSN_DATE. Date of displaced threshold position information.</summary>
    [Column("displaced_thr_position_date")]
    public DateTime? DisplacedThrPositionDate { get; set; }

    /// <summary>FAA NASR field: DSPL_THR_ELEV_SOURCE. Source of displaced threshold elevation information.</summary>
    [Column("displaced_thr_elevation_source")]
    public string? DisplacedThrElevationSource { get; set; }

    /// <summary>FAA NASR field: RWY_END_DSPL_THR_ELEV_DATE. Date of displaced threshold elevation information.</summary>
    [Column("displaced_thr_elevation_date")]
    public DateTime? DisplacedThrElevationDate { get; set; }

    /// <summary>FAA NASR field: TDZ_ELEV_SOURCE. Source of touchdown zone elevation information.</summary>
    [Column("touchdown_zone_elev_source")]
    public string? TouchdownZoneElevSource { get; set; }

    /// <summary>FAA NASR field: RWY_END_TDZ_ELEV_DATE. Date of touchdown zone elevation information.</summary>
    [Column("touchdown_zone_elev_date")]
    public DateTime? TouchdownZoneElevDate { get; set; }

    // Declared Distances
    /// <summary>FAA NASR field: TKOF_RUN_AVBL. Takeoff Run Available (TORA) in feet.</summary>
    [Column("takeoff_run_available")]
    public int? TakeoffRunAvailable { get; set; }

    /// <summary>FAA NASR field: TKOF_DIST_AVBL. Takeoff Distance Available (TODA) in feet.</summary>
    [Column("takeoff_distance_available")]
    public int? TakeoffDistanceAvailable { get; set; }

    /// <summary>FAA NASR field: ACLT_STOP_DIST_AVBL. Accelerate-Stop Distance Available (ASDA) in feet.</summary>
    [Column("accelerate_stop_dist_available")]
    public int? AccelerateStopDistAvailable { get; set; }

    /// <summary>FAA NASR field: LNDG_DIST_AVBL. Landing Distance Available (LDA) in feet.</summary>
    [Column("landing_distance_available")]
    public int? LandingDistanceAvailable { get; set; }

    // LAHSO (Land and Hold Short Operations)
    /// <summary>FAA NASR field: LAHSO_ALD. Available Landing Distance for Land and Hold Short Operations (LAHSO), in feet.</summary>
    [Column("lahso_available_landing_distance")]
    public int? LahsoAvailableLandingDistance { get; set; }

    /// <summary>FAA NASR field: RWY_END_INTERSECT_LAHSO. ID of Intersecting Runway Defining Hold Short Point.</summary>
    [Column("lahso_intersecting_runway")]
    public string? LahsoIntersectingRunway { get; set; }

    /// <summary>FAA NASR field: LAHSO_DESC. Description of Entity Defining Hold Short Point if not an Intersecting Runway.</summary>
    [Column("lahso_description")]
    public string? LahsoDescription { get; set; }

    /// <summary>FAA NASR field: LAHSO_LAT. LAHSO hold short point latitude (DMS format).</summary>
    [Column("lahso_latitude")]
    public string? LahsoLatitude { get; set; }

    /// <summary>FAA NASR field: LAT_LAHSO_DECIMAL. LAHSO hold short point latitude in decimal degrees.</summary>
    [Column("lahso_lat_decimal", TypeName = "decimal(10,8)")]
    public decimal? LahsoLatDecimal { get; set; }

    /// <summary>FAA NASR field: LAHSO_LONG. LAHSO hold short point longitude (DMS format).</summary>
    [Column("lahso_longitude")]
    public string? LahsoLongitude { get; set; }

    /// <summary>FAA NASR field: LONG_LAHSO_DECIMAL. LAHSO hold short point longitude in decimal degrees.</summary>
    [Column("lahso_long_decimal", TypeName = "decimal(11,8)")]
    public decimal? LahsoLongDecimal { get; set; }

    /// <summary>FAA NASR field: LAHSO_PSN_SOURCE. Source of LAHSO position information.</summary>
    [Column("lahso_position_source")]
    public string? LahsoPositionSource { get; set; }

    /// <summary>FAA NASR field: RWY_END_LAHSO_PSN_DATE. Date of LAHSO position information.</summary>
    [Column("lahso_position_date")]
    public DateTime? LahsoPositionDate { get; set; }

    /// <summary>Navigation property to the parent Runway entity.</summary>
    [ForeignKey(nameof(RunwayFk))]
    public virtual Runway? Runway { get; set; }

    // INasrEntity<RunwayEnd> implementation
    public string CreateUniqueKey()
    {
        return string.Join("|", SiteNo, RunwayIdRef, RunwayEndId);
    }

    public void UpdateFrom(RunwayEnd source, HashSet<string>? limitToProperties = null)
    {
        if (limitToProperties == null || !limitToProperties.Any())
        {
            UpdateAllProperties(source);
        }
        else
        {
            UpdateSelectiveProperties(source, limitToProperties);
        }
    }

    public RunwayEnd CreateSelectiveEntity(HashSet<string> properties)
    {
        var selective = new RunwayEnd();

        if (properties.Contains(nameof(SiteNo)))
            selective.SiteNo = SiteNo;
        if (properties.Contains(nameof(RunwayIdRef)))
            selective.RunwayIdRef = RunwayIdRef;
        if (properties.Contains(nameof(RunwayEndId)))
            selective.RunwayEndId = RunwayEndId;
        if (properties.Contains(nameof(TrueAlignment)))
            selective.TrueAlignment = TrueAlignment;
        if (properties.Contains(nameof(ApproachType)))
            selective.ApproachType = ApproachType;
        if (properties.Contains(nameof(RightHandTrafficPattern)))
            selective.RightHandTrafficPattern = RightHandTrafficPattern;
        if (properties.Contains(nameof(RunwayMarkingsType)))
            selective.RunwayMarkingsType = RunwayMarkingsType;
        if (properties.Contains(nameof(RunwayMarkingsCondition)))
            selective.RunwayMarkingsCondition = RunwayMarkingsCondition;
        if (properties.Contains(nameof(LatDecimal)))
            selective.LatDecimal = LatDecimal;
        if (properties.Contains(nameof(LongDecimal)))
            selective.LongDecimal = LongDecimal;
        if (properties.Contains(nameof(Elevation)))
            selective.Elevation = Elevation;
        if (properties.Contains(nameof(ThresholdCrossingHeight)))
            selective.ThresholdCrossingHeight = ThresholdCrossingHeight;
        if (properties.Contains(nameof(VisualGlidePathAngle)))
            selective.VisualGlidePathAngle = VisualGlidePathAngle;
        if (properties.Contains(nameof(DisplacedThresholdLatDecimal)))
            selective.DisplacedThresholdLatDecimal = DisplacedThresholdLatDecimal;
        if (properties.Contains(nameof(DisplacedThresholdLongDecimal)))
            selective.DisplacedThresholdLongDecimal = DisplacedThresholdLongDecimal;
        if (properties.Contains(nameof(DisplacedThresholdElev)))
            selective.DisplacedThresholdElev = DisplacedThresholdElev;
        if (properties.Contains(nameof(DisplacedThresholdLength)))
            selective.DisplacedThresholdLength = DisplacedThresholdLength;
        if (properties.Contains(nameof(TouchdownZoneElev)))
            selective.TouchdownZoneElev = TouchdownZoneElev;
        if (properties.Contains(nameof(VisualGlideSlopeIndicator)))
            selective.VisualGlideSlopeIndicator = VisualGlideSlopeIndicator;
        if (properties.Contains(nameof(RunwayVisualRangeEquipment)))
            selective.RunwayVisualRangeEquipment = RunwayVisualRangeEquipment;
        if (properties.Contains(nameof(RunwayVisibilityValueEquipment)))
            selective.RunwayVisibilityValueEquipment = RunwayVisibilityValueEquipment;
        if (properties.Contains(nameof(ApproachLightSystem)))
            selective.ApproachLightSystem = ApproachLightSystem;
        if (properties.Contains(nameof(RunwayEndLights)))
            selective.RunwayEndLights = RunwayEndLights;
        if (properties.Contains(nameof(CenterlineLights)))
            selective.CenterlineLights = CenterlineLights;
        if (properties.Contains(nameof(TouchdownZoneLights)))
            selective.TouchdownZoneLights = TouchdownZoneLights;
        if (properties.Contains(nameof(ControllingObjectDescription)))
            selective.ControllingObjectDescription = ControllingObjectDescription;
        if (properties.Contains(nameof(ControllingObjectMarkedLighted)))
            selective.ControllingObjectMarkedLighted = ControllingObjectMarkedLighted;
        if (properties.Contains(nameof(ControllingObjectClearanceSlope)))
            selective.ControllingObjectClearanceSlope = ControllingObjectClearanceSlope;
        if (properties.Contains(nameof(ControllingObjectHeightAboveRunway)))
            selective.ControllingObjectHeightAboveRunway = ControllingObjectHeightAboveRunway;
        if (properties.Contains(nameof(ControllingObjectDistanceFromRunway)))
            selective.ControllingObjectDistanceFromRunway = ControllingObjectDistanceFromRunway;
        if (properties.Contains(nameof(ControllingObjectCenterlineOffset)))
            selective.ControllingObjectCenterlineOffset = ControllingObjectCenterlineOffset;

        // DMS Coordinates - Runway End
        if (properties.Contains(nameof(RwyEndLatDeg)))
            selective.RwyEndLatDeg = RwyEndLatDeg;
        if (properties.Contains(nameof(RwyEndLatMin)))
            selective.RwyEndLatMin = RwyEndLatMin;
        if (properties.Contains(nameof(RwyEndLatSec)))
            selective.RwyEndLatSec = RwyEndLatSec;
        if (properties.Contains(nameof(RwyEndLatHemis)))
            selective.RwyEndLatHemis = RwyEndLatHemis;
        if (properties.Contains(nameof(RwyEndLongDeg)))
            selective.RwyEndLongDeg = RwyEndLongDeg;
        if (properties.Contains(nameof(RwyEndLongMin)))
            selective.RwyEndLongMin = RwyEndLongMin;
        if (properties.Contains(nameof(RwyEndLongSec)))
            selective.RwyEndLongSec = RwyEndLongSec;
        if (properties.Contains(nameof(RwyEndLongHemis)))
            selective.RwyEndLongHemis = RwyEndLongHemis;
        if (properties.Contains(nameof(DisplacedThrLatDeg)))
            selective.DisplacedThrLatDeg = DisplacedThrLatDeg;
        if (properties.Contains(nameof(DisplacedThrLatMin)))
            selective.DisplacedThrLatMin = DisplacedThrLatMin;
        if (properties.Contains(nameof(DisplacedThrLatSec)))
            selective.DisplacedThrLatSec = DisplacedThrLatSec;
        if (properties.Contains(nameof(DisplacedThrLatHemis)))
            selective.DisplacedThrLatHemis = DisplacedThrLatHemis;
        if (properties.Contains(nameof(DisplacedThrLongDeg)))
            selective.DisplacedThrLongDeg = DisplacedThrLongDeg;
        if (properties.Contains(nameof(DisplacedThrLongMin)))
            selective.DisplacedThrLongMin = DisplacedThrLongMin;
        if (properties.Contains(nameof(DisplacedThrLongSec)))
            selective.DisplacedThrLongSec = DisplacedThrLongSec;
        if (properties.Contains(nameof(DisplacedThrLongHemis)))
            selective.DisplacedThrLongHemis = DisplacedThrLongHemis;

        // Codes & Gradient
        if (properties.Contains(nameof(FarPart77Code)))
            selective.FarPart77Code = FarPart77Code;
        if (properties.Contains(nameof(CenterlineDirectionCode)))
            selective.CenterlineDirectionCode = CenterlineDirectionCode;
        if (properties.Contains(nameof(RunwayGradient)))
            selective.RunwayGradient = RunwayGradient;
        if (properties.Contains(nameof(RunwayGradientDirection)))
            selective.RunwayGradientDirection = RunwayGradientDirection;

        // Source/Date Metadata
        if (properties.Contains(nameof(RwyEndPositionSource)))
            selective.RwyEndPositionSource = RwyEndPositionSource;
        if (properties.Contains(nameof(RwyEndPositionDate)))
            selective.RwyEndPositionDate = RwyEndPositionDate;
        if (properties.Contains(nameof(RwyEndElevationSource)))
            selective.RwyEndElevationSource = RwyEndElevationSource;
        if (properties.Contains(nameof(RwyEndElevationDate)))
            selective.RwyEndElevationDate = RwyEndElevationDate;
        if (properties.Contains(nameof(DisplacedThrPositionSource)))
            selective.DisplacedThrPositionSource = DisplacedThrPositionSource;
        if (properties.Contains(nameof(DisplacedThrPositionDate)))
            selective.DisplacedThrPositionDate = DisplacedThrPositionDate;
        if (properties.Contains(nameof(DisplacedThrElevationSource)))
            selective.DisplacedThrElevationSource = DisplacedThrElevationSource;
        if (properties.Contains(nameof(DisplacedThrElevationDate)))
            selective.DisplacedThrElevationDate = DisplacedThrElevationDate;
        if (properties.Contains(nameof(TouchdownZoneElevSource)))
            selective.TouchdownZoneElevSource = TouchdownZoneElevSource;
        if (properties.Contains(nameof(TouchdownZoneElevDate)))
            selective.TouchdownZoneElevDate = TouchdownZoneElevDate;

        // Declared Distances
        if (properties.Contains(nameof(TakeoffRunAvailable)))
            selective.TakeoffRunAvailable = TakeoffRunAvailable;
        if (properties.Contains(nameof(TakeoffDistanceAvailable)))
            selective.TakeoffDistanceAvailable = TakeoffDistanceAvailable;
        if (properties.Contains(nameof(AccelerateStopDistAvailable)))
            selective.AccelerateStopDistAvailable = AccelerateStopDistAvailable;
        if (properties.Contains(nameof(LandingDistanceAvailable)))
            selective.LandingDistanceAvailable = LandingDistanceAvailable;

        // LAHSO
        if (properties.Contains(nameof(LahsoAvailableLandingDistance)))
            selective.LahsoAvailableLandingDistance = LahsoAvailableLandingDistance;
        if (properties.Contains(nameof(LahsoIntersectingRunway)))
            selective.LahsoIntersectingRunway = LahsoIntersectingRunway;
        if (properties.Contains(nameof(LahsoDescription)))
            selective.LahsoDescription = LahsoDescription;
        if (properties.Contains(nameof(LahsoLatitude)))
            selective.LahsoLatitude = LahsoLatitude;
        if (properties.Contains(nameof(LahsoLatDecimal)))
            selective.LahsoLatDecimal = LahsoLatDecimal;
        if (properties.Contains(nameof(LahsoLongitude)))
            selective.LahsoLongitude = LahsoLongitude;
        if (properties.Contains(nameof(LahsoLongDecimal)))
            selective.LahsoLongDecimal = LahsoLongDecimal;
        if (properties.Contains(nameof(LahsoPositionSource)))
            selective.LahsoPositionSource = LahsoPositionSource;
        if (properties.Contains(nameof(LahsoPositionDate)))
            selective.LahsoPositionDate = LahsoPositionDate;

        return selective;
    }

    private void UpdateAllProperties(RunwayEnd source)
    {
        TrueAlignment = source.TrueAlignment;
        ApproachType = source.ApproachType;
        RightHandTrafficPattern = source.RightHandTrafficPattern;
        RunwayMarkingsType = source.RunwayMarkingsType;
        RunwayMarkingsCondition = source.RunwayMarkingsCondition;
        LatDecimal = source.LatDecimal;
        LongDecimal = source.LongDecimal;
        Elevation = source.Elevation;
        ThresholdCrossingHeight = source.ThresholdCrossingHeight;
        VisualGlidePathAngle = source.VisualGlidePathAngle;
        DisplacedThresholdLatDecimal = source.DisplacedThresholdLatDecimal;
        DisplacedThresholdLongDecimal = source.DisplacedThresholdLongDecimal;
        DisplacedThresholdElev = source.DisplacedThresholdElev;
        DisplacedThresholdLength = source.DisplacedThresholdLength;
        TouchdownZoneElev = source.TouchdownZoneElev;
        VisualGlideSlopeIndicator = source.VisualGlideSlopeIndicator;
        RunwayVisualRangeEquipment = source.RunwayVisualRangeEquipment;
        RunwayVisibilityValueEquipment = source.RunwayVisibilityValueEquipment;
        ApproachLightSystem = source.ApproachLightSystem;
        RunwayEndLights = source.RunwayEndLights;
        CenterlineLights = source.CenterlineLights;
        TouchdownZoneLights = source.TouchdownZoneLights;
        ControllingObjectDescription = source.ControllingObjectDescription;
        ControllingObjectMarkedLighted = source.ControllingObjectMarkedLighted;
        ControllingObjectClearanceSlope = source.ControllingObjectClearanceSlope;
        ControllingObjectHeightAboveRunway = source.ControllingObjectHeightAboveRunway;
        ControllingObjectDistanceFromRunway = source.ControllingObjectDistanceFromRunway;
        ControllingObjectCenterlineOffset = source.ControllingObjectCenterlineOffset;

        // DMS Coordinates - Runway End
        RwyEndLatDeg = source.RwyEndLatDeg;
        RwyEndLatMin = source.RwyEndLatMin;
        RwyEndLatSec = source.RwyEndLatSec;
        RwyEndLatHemis = source.RwyEndLatHemis;
        RwyEndLongDeg = source.RwyEndLongDeg;
        RwyEndLongMin = source.RwyEndLongMin;
        RwyEndLongSec = source.RwyEndLongSec;
        RwyEndLongHemis = source.RwyEndLongHemis;
        DisplacedThrLatDeg = source.DisplacedThrLatDeg;
        DisplacedThrLatMin = source.DisplacedThrLatMin;
        DisplacedThrLatSec = source.DisplacedThrLatSec;
        DisplacedThrLatHemis = source.DisplacedThrLatHemis;
        DisplacedThrLongDeg = source.DisplacedThrLongDeg;
        DisplacedThrLongMin = source.DisplacedThrLongMin;
        DisplacedThrLongSec = source.DisplacedThrLongSec;
        DisplacedThrLongHemis = source.DisplacedThrLongHemis;

        // Codes & Gradient
        FarPart77Code = source.FarPart77Code;
        CenterlineDirectionCode = source.CenterlineDirectionCode;
        RunwayGradient = source.RunwayGradient;
        RunwayGradientDirection = source.RunwayGradientDirection;

        // Source/Date Metadata
        RwyEndPositionSource = source.RwyEndPositionSource;
        RwyEndPositionDate = source.RwyEndPositionDate;
        RwyEndElevationSource = source.RwyEndElevationSource;
        RwyEndElevationDate = source.RwyEndElevationDate;
        DisplacedThrPositionSource = source.DisplacedThrPositionSource;
        DisplacedThrPositionDate = source.DisplacedThrPositionDate;
        DisplacedThrElevationSource = source.DisplacedThrElevationSource;
        DisplacedThrElevationDate = source.DisplacedThrElevationDate;
        TouchdownZoneElevSource = source.TouchdownZoneElevSource;
        TouchdownZoneElevDate = source.TouchdownZoneElevDate;

        // Declared Distances
        TakeoffRunAvailable = source.TakeoffRunAvailable;
        TakeoffDistanceAvailable = source.TakeoffDistanceAvailable;
        AccelerateStopDistAvailable = source.AccelerateStopDistAvailable;
        LandingDistanceAvailable = source.LandingDistanceAvailable;

        // LAHSO
        LahsoAvailableLandingDistance = source.LahsoAvailableLandingDistance;
        LahsoIntersectingRunway = source.LahsoIntersectingRunway;
        LahsoDescription = source.LahsoDescription;
        LahsoLatitude = source.LahsoLatitude;
        LahsoLatDecimal = source.LahsoLatDecimal;
        LahsoLongitude = source.LahsoLongitude;
        LahsoLongDecimal = source.LahsoLongDecimal;
        LahsoPositionSource = source.LahsoPositionSource;
        LahsoPositionDate = source.LahsoPositionDate;
    }

    private void UpdateSelectiveProperties(RunwayEnd source, HashSet<string> limitToProperties)
    {
        if (limitToProperties.Contains(nameof(TrueAlignment)) && source.TrueAlignment != null)
            TrueAlignment = source.TrueAlignment;
        if (limitToProperties.Contains(nameof(ApproachType)) && source.ApproachType != null)
            ApproachType = source.ApproachType;
        if (limitToProperties.Contains(nameof(RightHandTrafficPattern)))
            RightHandTrafficPattern = source.RightHandTrafficPattern;
        if (limitToProperties.Contains(nameof(RunwayMarkingsType)) && source.RunwayMarkingsType != null)
            RunwayMarkingsType = source.RunwayMarkingsType;
        if (limitToProperties.Contains(nameof(RunwayMarkingsCondition)) && source.RunwayMarkingsCondition != null)
            RunwayMarkingsCondition = source.RunwayMarkingsCondition;
        if (limitToProperties.Contains(nameof(LatDecimal)) && source.LatDecimal != null)
            LatDecimal = source.LatDecimal;
        if (limitToProperties.Contains(nameof(LongDecimal)) && source.LongDecimal != null)
            LongDecimal = source.LongDecimal;
        if (limitToProperties.Contains(nameof(Elevation)) && source.Elevation != null)
            Elevation = source.Elevation;
        if (limitToProperties.Contains(nameof(ThresholdCrossingHeight)) && source.ThresholdCrossingHeight != null)
            ThresholdCrossingHeight = source.ThresholdCrossingHeight;
        if (limitToProperties.Contains(nameof(VisualGlidePathAngle)) && source.VisualGlidePathAngle != null)
            VisualGlidePathAngle = source.VisualGlidePathAngle;
        if (limitToProperties.Contains(nameof(DisplacedThresholdLatDecimal)) && source.DisplacedThresholdLatDecimal != null)
            DisplacedThresholdLatDecimal = source.DisplacedThresholdLatDecimal;
        if (limitToProperties.Contains(nameof(DisplacedThresholdLongDecimal)) && source.DisplacedThresholdLongDecimal != null)
            DisplacedThresholdLongDecimal = source.DisplacedThresholdLongDecimal;
        if (limitToProperties.Contains(nameof(DisplacedThresholdElev)) && source.DisplacedThresholdElev != null)
            DisplacedThresholdElev = source.DisplacedThresholdElev;
        if (limitToProperties.Contains(nameof(DisplacedThresholdLength)) && source.DisplacedThresholdLength != null)
            DisplacedThresholdLength = source.DisplacedThresholdLength;
        if (limitToProperties.Contains(nameof(TouchdownZoneElev)) && source.TouchdownZoneElev != null)
            TouchdownZoneElev = source.TouchdownZoneElev;
        if (limitToProperties.Contains(nameof(VisualGlideSlopeIndicator)) && source.VisualGlideSlopeIndicator != null)
            VisualGlideSlopeIndicator = source.VisualGlideSlopeIndicator;
        if (limitToProperties.Contains(nameof(RunwayVisualRangeEquipment)) && source.RunwayVisualRangeEquipment != null)
            RunwayVisualRangeEquipment = source.RunwayVisualRangeEquipment;
        if (limitToProperties.Contains(nameof(RunwayVisibilityValueEquipment)))
            RunwayVisibilityValueEquipment = source.RunwayVisibilityValueEquipment;
        if (limitToProperties.Contains(nameof(ApproachLightSystem)) && source.ApproachLightSystem != null)
            ApproachLightSystem = source.ApproachLightSystem;
        if (limitToProperties.Contains(nameof(RunwayEndLights)))
            RunwayEndLights = source.RunwayEndLights;
        if (limitToProperties.Contains(nameof(CenterlineLights)))
            CenterlineLights = source.CenterlineLights;
        if (limitToProperties.Contains(nameof(TouchdownZoneLights)))
            TouchdownZoneLights = source.TouchdownZoneLights;
        if (limitToProperties.Contains(nameof(ControllingObjectDescription)) && source.ControllingObjectDescription != null)
            ControllingObjectDescription = source.ControllingObjectDescription;
        if (limitToProperties.Contains(nameof(ControllingObjectMarkedLighted)) && source.ControllingObjectMarkedLighted != null)
            ControllingObjectMarkedLighted = source.ControllingObjectMarkedLighted;
        if (limitToProperties.Contains(nameof(ControllingObjectClearanceSlope)) && source.ControllingObjectClearanceSlope != null)
            ControllingObjectClearanceSlope = source.ControllingObjectClearanceSlope;
        if (limitToProperties.Contains(nameof(ControllingObjectHeightAboveRunway)) && source.ControllingObjectHeightAboveRunway != null)
            ControllingObjectHeightAboveRunway = source.ControllingObjectHeightAboveRunway;
        if (limitToProperties.Contains(nameof(ControllingObjectDistanceFromRunway)) && source.ControllingObjectDistanceFromRunway != null)
            ControllingObjectDistanceFromRunway = source.ControllingObjectDistanceFromRunway;
        if (limitToProperties.Contains(nameof(ControllingObjectCenterlineOffset)) && source.ControllingObjectCenterlineOffset != null)
            ControllingObjectCenterlineOffset = source.ControllingObjectCenterlineOffset;

        // DMS Coordinates - Runway End
        if (limitToProperties.Contains(nameof(RwyEndLatDeg)) && source.RwyEndLatDeg != null)
            RwyEndLatDeg = source.RwyEndLatDeg;
        if (limitToProperties.Contains(nameof(RwyEndLatMin)) && source.RwyEndLatMin != null)
            RwyEndLatMin = source.RwyEndLatMin;
        if (limitToProperties.Contains(nameof(RwyEndLatSec)) && source.RwyEndLatSec != null)
            RwyEndLatSec = source.RwyEndLatSec;
        if (limitToProperties.Contains(nameof(RwyEndLatHemis)) && source.RwyEndLatHemis != null)
            RwyEndLatHemis = source.RwyEndLatHemis;
        if (limitToProperties.Contains(nameof(RwyEndLongDeg)) && source.RwyEndLongDeg != null)
            RwyEndLongDeg = source.RwyEndLongDeg;
        if (limitToProperties.Contains(nameof(RwyEndLongMin)) && source.RwyEndLongMin != null)
            RwyEndLongMin = source.RwyEndLongMin;
        if (limitToProperties.Contains(nameof(RwyEndLongSec)) && source.RwyEndLongSec != null)
            RwyEndLongSec = source.RwyEndLongSec;
        if (limitToProperties.Contains(nameof(RwyEndLongHemis)) && source.RwyEndLongHemis != null)
            RwyEndLongHemis = source.RwyEndLongHemis;
        if (limitToProperties.Contains(nameof(DisplacedThrLatDeg)) && source.DisplacedThrLatDeg != null)
            DisplacedThrLatDeg = source.DisplacedThrLatDeg;
        if (limitToProperties.Contains(nameof(DisplacedThrLatMin)) && source.DisplacedThrLatMin != null)
            DisplacedThrLatMin = source.DisplacedThrLatMin;
        if (limitToProperties.Contains(nameof(DisplacedThrLatSec)) && source.DisplacedThrLatSec != null)
            DisplacedThrLatSec = source.DisplacedThrLatSec;
        if (limitToProperties.Contains(nameof(DisplacedThrLatHemis)) && source.DisplacedThrLatHemis != null)
            DisplacedThrLatHemis = source.DisplacedThrLatHemis;
        if (limitToProperties.Contains(nameof(DisplacedThrLongDeg)) && source.DisplacedThrLongDeg != null)
            DisplacedThrLongDeg = source.DisplacedThrLongDeg;
        if (limitToProperties.Contains(nameof(DisplacedThrLongMin)) && source.DisplacedThrLongMin != null)
            DisplacedThrLongMin = source.DisplacedThrLongMin;
        if (limitToProperties.Contains(nameof(DisplacedThrLongSec)) && source.DisplacedThrLongSec != null)
            DisplacedThrLongSec = source.DisplacedThrLongSec;
        if (limitToProperties.Contains(nameof(DisplacedThrLongHemis)) && source.DisplacedThrLongHemis != null)
            DisplacedThrLongHemis = source.DisplacedThrLongHemis;

        // Codes & Gradient
        if (limitToProperties.Contains(nameof(FarPart77Code)) && source.FarPart77Code != null)
            FarPart77Code = source.FarPart77Code;
        if (limitToProperties.Contains(nameof(CenterlineDirectionCode)) && source.CenterlineDirectionCode != null)
            CenterlineDirectionCode = source.CenterlineDirectionCode;
        if (limitToProperties.Contains(nameof(RunwayGradient)) && source.RunwayGradient != null)
            RunwayGradient = source.RunwayGradient;
        if (limitToProperties.Contains(nameof(RunwayGradientDirection)) && source.RunwayGradientDirection != null)
            RunwayGradientDirection = source.RunwayGradientDirection;

        // Source/Date Metadata
        if (limitToProperties.Contains(nameof(RwyEndPositionSource)) && source.RwyEndPositionSource != null)
            RwyEndPositionSource = source.RwyEndPositionSource;
        if (limitToProperties.Contains(nameof(RwyEndPositionDate)) && source.RwyEndPositionDate != null)
            RwyEndPositionDate = source.RwyEndPositionDate;
        if (limitToProperties.Contains(nameof(RwyEndElevationSource)) && source.RwyEndElevationSource != null)
            RwyEndElevationSource = source.RwyEndElevationSource;
        if (limitToProperties.Contains(nameof(RwyEndElevationDate)) && source.RwyEndElevationDate != null)
            RwyEndElevationDate = source.RwyEndElevationDate;
        if (limitToProperties.Contains(nameof(DisplacedThrPositionSource)) && source.DisplacedThrPositionSource != null)
            DisplacedThrPositionSource = source.DisplacedThrPositionSource;
        if (limitToProperties.Contains(nameof(DisplacedThrPositionDate)) && source.DisplacedThrPositionDate != null)
            DisplacedThrPositionDate = source.DisplacedThrPositionDate;
        if (limitToProperties.Contains(nameof(DisplacedThrElevationSource)) && source.DisplacedThrElevationSource != null)
            DisplacedThrElevationSource = source.DisplacedThrElevationSource;
        if (limitToProperties.Contains(nameof(DisplacedThrElevationDate)) && source.DisplacedThrElevationDate != null)
            DisplacedThrElevationDate = source.DisplacedThrElevationDate;
        if (limitToProperties.Contains(nameof(TouchdownZoneElevSource)) && source.TouchdownZoneElevSource != null)
            TouchdownZoneElevSource = source.TouchdownZoneElevSource;
        if (limitToProperties.Contains(nameof(TouchdownZoneElevDate)) && source.TouchdownZoneElevDate != null)
            TouchdownZoneElevDate = source.TouchdownZoneElevDate;

        // Declared Distances
        if (limitToProperties.Contains(nameof(TakeoffRunAvailable)) && source.TakeoffRunAvailable != null)
            TakeoffRunAvailable = source.TakeoffRunAvailable;
        if (limitToProperties.Contains(nameof(TakeoffDistanceAvailable)) && source.TakeoffDistanceAvailable != null)
            TakeoffDistanceAvailable = source.TakeoffDistanceAvailable;
        if (limitToProperties.Contains(nameof(AccelerateStopDistAvailable)) && source.AccelerateStopDistAvailable != null)
            AccelerateStopDistAvailable = source.AccelerateStopDistAvailable;
        if (limitToProperties.Contains(nameof(LandingDistanceAvailable)) && source.LandingDistanceAvailable != null)
            LandingDistanceAvailable = source.LandingDistanceAvailable;

        // LAHSO
        if (limitToProperties.Contains(nameof(LahsoAvailableLandingDistance)) && source.LahsoAvailableLandingDistance != null)
            LahsoAvailableLandingDistance = source.LahsoAvailableLandingDistance;
        if (limitToProperties.Contains(nameof(LahsoIntersectingRunway)) && source.LahsoIntersectingRunway != null)
            LahsoIntersectingRunway = source.LahsoIntersectingRunway;
        if (limitToProperties.Contains(nameof(LahsoDescription)) && source.LahsoDescription != null)
            LahsoDescription = source.LahsoDescription;
        if (limitToProperties.Contains(nameof(LahsoLatitude)) && source.LahsoLatitude != null)
            LahsoLatitude = source.LahsoLatitude;
        if (limitToProperties.Contains(nameof(LahsoLatDecimal)) && source.LahsoLatDecimal != null)
            LahsoLatDecimal = source.LahsoLatDecimal;
        if (limitToProperties.Contains(nameof(LahsoLongitude)) && source.LahsoLongitude != null)
            LahsoLongitude = source.LahsoLongitude;
        if (limitToProperties.Contains(nameof(LahsoLongDecimal)) && source.LahsoLongDecimal != null)
            LahsoLongDecimal = source.LahsoLongDecimal;
        if (limitToProperties.Contains(nameof(LahsoPositionSource)) && source.LahsoPositionSource != null)
            LahsoPositionSource = source.LahsoPositionSource;
        if (limitToProperties.Contains(nameof(LahsoPositionDate)) && source.LahsoPositionDate != null)
            LahsoPositionDate = source.LahsoPositionDate;
    }
}

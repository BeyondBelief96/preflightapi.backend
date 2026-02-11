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
    [Column("site_no", TypeName = "varchar(9)")]
    [Required]
    public string SiteNo { get; set; } = string.Empty;

    /// <summary>FAA NASR field: RWY_ID. Runway identification of the parent runway.</summary>
    [Column("runway_id_ref", TypeName = "varchar(7)")]
    [Required]
    public string RunwayIdRef { get; set; } = string.Empty;

    /// <summary>Foreign key to the parent Runway entity. Populated after data sync by linking on SiteNo and RunwayIdRef.</summary>
    [Column("runway_fk")]
    public Guid? RunwayFk { get; set; }

    /// <summary>FAA NASR field: RWY_END_ID. Runway end identifier (e.g., "01", "19", "09L", "27R").</summary>
    [Column("runway_end_id", TypeName = "varchar(3)")]
    [Required]
    public string RunwayEndId { get; set; } = string.Empty;

    /// <summary>FAA NASR field: TRUE_ALIGNMENT. Runway end true alignment. True heading of the runway to the nearest degree.</summary>
    [Column("true_alignment")]
    public int? TrueAlignment { get; set; }

    /// <summary>
    /// FAA NASR field: ILS_TYPE. Instrument Landing System (ILS) type.
    /// <para>Possible values: ILS, MLS, SDF, LOCALIZER, LDA, ISMLS, ILS/DME, SDF/DME, LOC/DME, LOC/GS, LDA/DME.</para>
    /// </summary>
    [Column("approach_type", TypeName = "varchar(10)")]
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
    [Column("runway_markings_type", TypeName = "varchar(5)")]
    public string? RunwayMarkingsType { get; set; }

    /// <summary>
    /// FAA NASR field: RWY_MARKING_COND. Runway markings condition.
    /// <para>Possible values: G (Good), F (Fair), P (Poor).</para>
    /// </summary>
    [Column("runway_markings_condition", TypeName = "varchar(1)")]
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
    [Column("visual_glide_slope_indicator", TypeName = "varchar(5)")]
    public string? VisualGlideSlopeIndicator { get; set; }

    /// <summary>
    /// FAA NASR field: RWY_VISUAL_RANGE_EQUIP_CODE. Runway Visual Range (RVR) equipment location.
    /// Indicates location(s) at which RVR equipment is installed.
    /// <para>Possible values: T (Touchdown), M (Midfield), R (Rollout), N (No RVR Available),
    /// TM, TR, MR, TMR (combinations).</para>
    /// </summary>
    [Column("runway_visual_range_equipment", TypeName = "varchar(3)")]
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
    [Column("approach_light_system", TypeName = "varchar(8)")]
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
    [Column("controlling_object_description", TypeName = "varchar(11)")]
    public string? ControllingObjectDescription { get; set; }

    /// <summary>
    /// FAA NASR field: OBSTN_MRKD_CODE. Controlling object marked/lighted status.
    /// <para>Possible values: M (Marked), L (Lighted), ML (Marked and Lighted), NONE.</para>
    /// </summary>
    [Column("controlling_object_marked_lighted", TypeName = "varchar(4)")]
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
    [Column("controlling_object_centerline_offset", TypeName = "varchar(7)")]
    public string? ControllingObjectCenterlineOffset { get; set; }

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
    }
}

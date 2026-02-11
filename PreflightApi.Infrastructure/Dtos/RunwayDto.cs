using PreflightApi.Domain.Enums;

namespace PreflightApi.Infrastructure.Dtos;

/// <summary>
/// Runway data from the FAA NASR database, sourced from APT_RWY.
/// Includes dimensions, surface, lighting, and weight-bearing information.
/// </summary>
public record RunwayDto
{
    /// <summary>System-generated unique identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>FAA NASR field: RWY_ID. Runway identification (e.g., "01/19", "09L/27R", "H1" for helipad).</summary>
    public string RunwayId { get; init; } = string.Empty;

    /// <summary>FAA NASR field: RWY_LEN. Physical runway length to the nearest foot.</summary>
    public int? Length { get; init; }

    /// <summary>FAA NASR field: RWY_WIDTH. Physical runway width to the nearest foot.</summary>
    public int? Width { get; init; }

    /// <summary>
    /// FAA NASR field: SURFACE_TYPE_CODE. Runway surface type.
    /// <para>Common values: Concrete (CONC), Asphalt (ASPH), Turf (TURF), Dirt (DIRT), Gravel (GRAVEL), Water (WATER).</para>
    /// </summary>
    public RunwaySurfaceType SurfaceType { get; init; }

    /// <summary>
    /// FAA NASR field: TREATMENT_CODE. Runway surface treatment.
    /// <para>Possible values: Grooved (GRVD), PorousFrictionCourse (PFC), AggregateFrictionSealCoat (AFSC),
    /// RubberizedFrictionSealCoat (RFSC), WireComb (WC), None (NONE).</para>
    /// </summary>
    public RunwaySurfaceTreatment SurfaceTreatment { get; init; }

    /// <summary>FAA NASR field: PCN. Pavement Classification Number. See FAA Advisory Circular 150/5335-5 for code definitions and PCN determination formula.</summary>
    public string? PavementClassification { get; init; }

    /// <summary>
    /// FAA NASR field: RWY_LGT_CODE. Runway lights edge intensity.
    /// <para>Possible values: High (HIGH), Medium (MED), Low (LOW), Flood (FLD), NonStandard (NSTD), Perimeter (PERI), Strobe (STRB), None (NONE).</para>
    /// </summary>
    public RunwayEdgeLightIntensity EdgeLightIntensity { get; init; }

    /// <summary>FAA NASR field: GROSS_WT_SW. Runway weight-bearing capacity for single wheel type landing gear, in pounds.</summary>
    public int? WeightBearingSingleWheel { get; init; }

    /// <summary>FAA NASR field: GROSS_WT_DW. Runway weight-bearing capacity for dual wheel type landing gear, in pounds.</summary>
    public int? WeightBearingDualWheel { get; init; }

    /// <summary>FAA NASR field: GROSS_WT_DTW. Runway weight-bearing capacity for two dual wheels in tandem type landing gear, in pounds.</summary>
    public int? WeightBearingDualTandem { get; init; }

    /// <summary>FAA NASR field: GROSS_WT_DDTW. Runway weight-bearing capacity for two dual wheels in tandem/two dual wheels in double tandem body gear type landing gear, in pounds.</summary>
    public int? WeightBearingDoubleDualTandem { get; init; }

    /// <summary>Runway end details for each direction (typically two per runway).</summary>
    public List<RunwayEndDto> RunwayEnds { get; init; } = new();
}

/// <summary>
/// Runway end data from the FAA NASR database, sourced from APT_RWY_END.
/// Includes approach, markings, lighting, and controlling obstacle information.
/// </summary>
public record RunwayEndDto
{
    /// <summary>System-generated unique identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>FAA NASR field: RWY_END_ID. Runway end identifier (e.g., "01", "19", "09L", "27R").</summary>
    public string RunwayEndId { get; init; } = string.Empty;

    /// <summary>FAA NASR field: TRUE_ALIGNMENT. Runway end true alignment. True heading of the runway to the nearest degree.</summary>
    public int? TrueAlignment { get; init; }

    /// <summary>
    /// FAA NASR field: ILS_TYPE. Instrument Landing System (ILS) type.
    /// <para>Possible values: Ils (ILS), Mls (MLS), Sdf (SDF), Localizer (LOCALIZER), Lda (LDA), Ismls (ISMLS),
    /// IlsDme (ILS/DME), SdfDme (SDF/DME), LocDme (LOC/DME), LocGs (LOC/GS), LdaDme (LDA/DME).</para>
    /// </summary>
    public InstrumentApproachType ApproachType { get; init; }

    /// <summary>
    /// FAA NASR field: RIGHT_HAND_TRAFFIC_PAT_FLAG. Whether right-hand traffic pattern is in effect for landing aircraft.
    /// </summary>
    public bool RightHandTrafficPattern { get; init; }

    /// <summary>
    /// FAA NASR field: RWY_MARKING_TYPE_CODE. Runway markings type.
    /// <para>Possible values: PrecisionInstrument (PIR), NonPrecisionInstrument (NPI), Basic (BSC),
    /// NumbersOnly (NRS), NonStandard (NSTD), Buoys (BUOY), Stol (STOL), None (NONE).</para>
    /// </summary>
    public RunwayMarkingsType MarkingsType { get; init; }

    /// <summary>
    /// FAA NASR field: RWY_MARKING_COND. Runway markings condition.
    /// <para>Possible values: Good (G), Fair (F), Poor (P).</para>
    /// </summary>
    public RunwayMarkingsCondition MarkingsCondition { get; init; }

    /// <summary>FAA NASR field: LAT_DECIMAL. Latitude of physical runway end in decimal degrees.</summary>
    public decimal? Latitude { get; init; }

    /// <summary>FAA NASR field: LONG_DECIMAL. Longitude of physical runway end in decimal degrees.</summary>
    public decimal? Longitude { get; init; }

    /// <summary>FAA NASR field: RWY_END_ELEV. Elevation at the physical runway end in feet MSL.</summary>
    public decimal? Elevation { get; init; }

    /// <summary>FAA NASR field: THR_CROSSING_HGT. Threshold Crossing Height in feet AGL. Height that the effective visual glide path crosses above the runway threshold.</summary>
    public decimal? ThresholdCrossingHeight { get; init; }

    /// <summary>FAA NASR field: VISUAL_GLIDE_PATH_ANGLE. Visual glide path angle in hundredths of degrees.</summary>
    public decimal? VisualGlidePathAngle { get; init; }

    /// <summary>FAA NASR field: LAT_DISPLACED_THR_DECIMAL. Latitude of displaced threshold in decimal degrees.</summary>
    public decimal? DisplacedThresholdLatitude { get; init; }

    /// <summary>FAA NASR field: LONG_DISPLACED_THR_DECIMAL. Longitude of displaced threshold in decimal degrees.</summary>
    public decimal? DisplacedThresholdLongitude { get; init; }

    /// <summary>FAA NASR field: DISPLACED_THR_ELEV. Elevation at the displaced threshold in feet MSL.</summary>
    public decimal? DisplacedThresholdElevation { get; init; }

    /// <summary>FAA NASR field: DISPLACED_THR_LEN. Displaced threshold length in feet from the runway end.</summary>
    public int? DisplacedThresholdLength { get; init; }

    /// <summary>FAA NASR field: TDZ_ELEV. Elevation at the touchdown zone in feet MSL.</summary>
    public decimal? TouchdownZoneElevation { get; init; }

    /// <summary>
    /// FAA NASR field: VGSI_CODE. Visual Glide Slope Indicator type.
    /// <para>Common types: VASI (V2L/V4L/etc.), PAPI (P2L/P4L/etc.), SAVASI (S2L/S2R), Tri-Color, Pulsating, Panel systems.</para>
    /// </summary>
    public VisualGlideSlopeIndicatorType VisualGlideSlopeIndicator { get; init; }

    /// <summary>
    /// FAA NASR field: RWY_VISUAL_RANGE_EQUIP_CODE. Runway Visual Range (RVR) equipment location.
    /// <para>Possible values: Touchdown (T), Midfield (M), Rollout (R), None (N), TouchdownMidfield (TM), TouchdownRollout (TR), MidfieldRollout (MR), TouchdownMidfieldRollout (TMR).</para>
    /// </summary>
    public RunwayVisualRangeEquipmentType RunwayVisualRangeEquipment { get; init; }

    /// <summary>
    /// FAA NASR field: RWY_VSBY_VALUE_EQUIP_FLAG. Whether Runway Visibility Value (RVV) equipment is installed.
    /// </summary>
    public bool RunwayVisibilityValueEquipment { get; init; }

    /// <summary>
    /// FAA NASR field: APCH_LGT_SYSTEM_CODE. Approach light system type.
    /// <para>See <see cref="ApproachLightSystemType"/> enum for all possible values and their FAA descriptions.</para>
    /// </summary>
    public ApproachLightSystemType ApproachLightSystem { get; init; }

    /// <summary>FAA NASR field: RWY_END_LGTS_FLAG. Whether Runway End Identifier Lights (REIL) are installed.</summary>
    public bool HasRunwayEndLights { get; init; }

    /// <summary>FAA NASR field: CNTRLN_LGTS_AVBL_FLAG. Whether runway centerline lights are installed.</summary>
    public bool HasCenterlineLights { get; init; }

    /// <summary>FAA NASR field: TDZ_LGT_AVBL_FLAG. Whether runway end touchdown zone lights are installed.</summary>
    public bool HasTouchdownZoneLights { get; init; }

    /// <summary>FAA NASR field: OBSTN_TYPE. Controlling object description (type of obstacle).</summary>
    public string? ControllingObjectDescription { get; init; }

    /// <summary>
    /// FAA NASR field: OBSTN_MRKD_CODE. Controlling object marked/lighted status.
    /// <para>Possible values: Marked (M), Lighted (L), MarkedAndLighted (ML), None (NONE).</para>
    /// </summary>
    public ControllingObjectMarking ControllingObjectMarking { get; init; }

    /// <summary>FAA NASR field: OBSTN_CLNC_SLOPE. Controlling object clearance slope value, expressed as a ratio of N:1. If greater than 50:1, then 50 is entered.</summary>
    public int? ControllingObjectClearanceSlope { get; init; }

    /// <summary>FAA NASR field: OBSTN_HGT. Controlling object height above the physical runway end in feet AGL.</summary>
    public int? ControllingObjectHeightAboveRunway { get; init; }

    /// <summary>FAA NASR field: DIST_FROM_THR. Controlling object distance from the physical runway end in feet.</summary>
    public int? ControllingObjectDistanceFromRunway { get; init; }

    /// <summary>FAA NASR field: CNTRLN_OFFSET. Controlling object centerline offset distance in feet from the extended runway centerline.</summary>
    public string? ControllingObjectCenterlineOffset { get; init; }
}

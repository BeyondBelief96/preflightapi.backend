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

    /// <summary>ICAO code of the parent airport (e.g., KDFW). Included when queried via the Runways endpoints.</summary>
    public string? AirportIcaoCode { get; init; }

    /// <summary>FAA identifier of the parent airport (e.g., DFW). Included when queried via the Runways endpoints.</summary>
    public string? AirportArptId { get; init; }

    /// <summary>Name of the parent airport. Included when queried via the Runways endpoints.</summary>
    public string? AirportName { get; init; }

    /// <summary>FAA NASR field: RWY_ID. Runway identification (e.g., "01/19", "09L/27R", "H1" for helipad).</summary>
    public string RunwayId { get; init; } = string.Empty;

    /// <summary>FAA NASR field: RWY_LEN. Physical runway length to the nearest foot.</summary>
    public int? Length { get; init; }

    /// <summary>FAA NASR field: RWY_WIDTH. Physical runway width to the nearest foot.</summary>
    public int? Width { get; init; }

    /// <summary>
    /// FAA NASR field: SURFACE_TYPE_CODE. Primary runway surface type.
    /// <para>When the runway is composed of distinct sections the FAA reports a combined code (e.g., "ASPH-CONC").
    /// This property contains the first (primary) surface type; see <see cref="SecondarySurfaceType"/> for the second.</para>
    /// <para>Common values: Concrete (CONC), Asphalt (ASPH), Turf (TURF), Dirt (DIRT), Gravel (GRAVEL), Water (WATER).</para>
    /// </summary>
    public RunwaySurfaceType? SurfaceType { get; init; }

    /// <summary>
    /// Secondary runway surface type, present only when the runway is composed of two distinct surface sections.
    /// <para>Derived from the FAA NASR SURFACE_TYPE_CODE combined format (e.g., "ASPH-CONC" → Secondary = Concrete).</para>
    /// </summary>
    public RunwaySurfaceType? SecondarySurfaceType { get; init; }

    /// <summary>
    /// FAA NASR field: TREATMENT_CODE. Runway surface treatment.
    /// <para>Possible values: Grooved (GRVD), PorousFrictionCourse (PFC), AggregateFrictionSealCoat (AFSC),
    /// RubberizedFrictionSealCoat (RFSC), WireComb (WC), None (NONE).</para>
    /// </summary>
    public RunwaySurfaceTreatment? SurfaceTreatment { get; init; }

    /// <summary>FAA NASR field: PCN. Pavement Classification Number. See FAA Advisory Circular 150/5335-5 for code definitions and PCN determination formula.</summary>
    public string? PavementClassification { get; init; }

    /// <summary>
    /// FAA NASR field: RWY_LGT_CODE. Runway lights edge intensity.
    /// <para>Possible values: High (HIGH), Medium (MED), Low (LOW), Flood (FLD), NonStandard (NSTD), Perimeter (PERI), Strobe (STRB), None (NONE).</para>
    /// </summary>
    public RunwayEdgeLightIntensity? EdgeLightIntensity { get; init; }

    /// <summary>FAA NASR field: GROSS_WT_SW. Runway weight-bearing capacity for single wheel type landing gear, in pounds.</summary>
    public int? WeightBearingSingleWheel { get; init; }

    /// <summary>FAA NASR field: GROSS_WT_DW. Runway weight-bearing capacity for dual wheel type landing gear, in pounds.</summary>
    public int? WeightBearingDualWheel { get; init; }

    /// <summary>FAA NASR field: GROSS_WT_DTW. Runway weight-bearing capacity for two dual wheels in tandem type landing gear, in pounds.</summary>
    public int? WeightBearingDualTandem { get; init; }

    /// <summary>FAA NASR field: GROSS_WT_DDTW. Runway weight-bearing capacity for two dual wheels in tandem/two dual wheels in double tandem body gear type landing gear, in pounds.</summary>
    public int? WeightBearingDoubleDualTandem { get; init; }

    /// <summary>
    /// FAA NASR field: COND. Runway Surface Condition.
    /// <para>Possible values: Excellent, Good, Fair, Poor, Failed.</para>
    /// </summary>
    public RunwaySurfaceCondition? SurfaceCondition { get; init; }

    /// <summary>
    /// FAA NASR field: PAVEMENT_TYPE_CODE. Pavement Type.
    /// <para>Possible values: Rigid (R), Flexible (F).</para>
    /// </summary>
    public PavementType? PavementType { get; init; }

    /// <summary>
    /// FAA NASR field: SUBGRADE_STRENGTH_CODE. Subgrade Strength category (part of PCN system).
    /// <para>Possible values: High (A), Medium (B), Low (C), UltraLow (D).</para>
    /// </summary>
    public SubgradeStrength? SubgradeStrength { get; init; }

    /// <summary>
    /// FAA NASR field: TIRE_PRES_CODE. Maximum allowable tire pressure category (part of PCN system).
    /// <para>Possible values: High/no limit (W), Medium/217 psi (X), Low/145 psi (Y), VeryLow/73 psi (Z).</para>
    /// </summary>
    public TirePressure? TirePressure { get; init; }

    /// <summary>
    /// FAA NASR field: DTRM_METHOD_CODE. Pavement strength determination method.
    /// <para>Possible values: Technical (T), UsingAircraft (U).</para>
    /// </summary>
    public PavementDeterminationMethod? DeterminationMethod { get; init; }

    /// <summary>FAA NASR field: RWY_LEN_SOURCE. Source of runway length information.</summary>
    public string? RunwayLengthSource { get; init; }

    /// <summary>FAA NASR field: LENGTH_SOURCE_DATE. Date of runway length source information. ISO 8601 UTC format.</summary>
    public DateTime? LengthSourceDate { get; init; }

    /// <summary>GeoJSON polygon geometry of the physical runway boundary. Only included when includeGeometry=true.</summary>
    public GeoJsonGeometry? Geometry { get; init; }

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
    public InstrumentApproachType? ApproachType { get; init; }

    /// <summary>
    /// FAA NASR field: RIGHT_HAND_TRAFFIC_PAT_FLAG. Whether right-hand traffic pattern is in effect for landing aircraft.
    /// </summary>
    public bool RightHandTrafficPattern { get; init; }

    /// <summary>
    /// FAA NASR field: RWY_MARKING_TYPE_CODE. Runway markings type.
    /// <para>Possible values: PrecisionInstrument (PIR), NonPrecisionInstrument (NPI), Basic (BSC),
    /// NumbersOnly (NRS), NonStandard (NSTD), Buoys (BUOY), Stol (STOL), None (NONE).</para>
    /// </summary>
    public RunwayMarkingsType? MarkingsType { get; init; }

    /// <summary>
    /// FAA NASR field: RWY_MARKING_COND. Runway markings condition.
    /// <para>Possible values: Good (G), Fair (F), Poor (P).</para>
    /// </summary>
    public RunwayMarkingsCondition? MarkingsCondition { get; init; }

    /// <summary>FAA NASR field: LAT_DECIMAL. Latitude of physical runway end in decimal degrees (WGS 84).</summary>
    public double? Latitude { get; init; }

    /// <summary>FAA NASR field: LONG_DECIMAL. Longitude of physical runway end in decimal degrees (WGS 84).</summary>
    public double? Longitude { get; init; }

    /// <summary>FAA NASR field: RWY_END_ELEV. Elevation at the physical runway end in feet MSL.</summary>
    public double? Elevation { get; init; }

    /// <summary>FAA NASR field: THR_CROSSING_HGT. Threshold Crossing Height in feet AGL. Height that the effective visual glide path crosses above the runway threshold.</summary>
    public double? ThresholdCrossingHeight { get; init; }

    /// <summary>FAA NASR field: VISUAL_GLIDE_PATH_ANGLE. Visual glide path angle in hundredths of degrees (e.g., 300 = 3.00°).</summary>
    public double? VisualGlidePathAngle { get; init; }

    /// <summary>FAA NASR field: LAT_DISPLACED_THR_DECIMAL. Latitude of displaced threshold in decimal degrees (WGS 84).</summary>
    public double? DisplacedThresholdLatitude { get; init; }

    /// <summary>FAA NASR field: LONG_DISPLACED_THR_DECIMAL. Longitude of displaced threshold in decimal degrees (WGS 84).</summary>
    public double? DisplacedThresholdLongitude { get; init; }

    /// <summary>FAA NASR field: DISPLACED_THR_ELEV. Elevation at the displaced threshold in feet MSL.</summary>
    public double? DisplacedThresholdElevation { get; init; }

    /// <summary>FAA NASR field: DISPLACED_THR_LEN. Displaced threshold length in feet from the runway end.</summary>
    public int? DisplacedThresholdLength { get; init; }

    /// <summary>FAA NASR field: TDZ_ELEV. Elevation at the touchdown zone in feet MSL.</summary>
    public double? TouchdownZoneElevation { get; init; }

    /// <summary>
    /// FAA NASR field: VGSI_CODE. Visual Glide Slope Indicator type.
    /// <para>Common types: VASI (V2L/V4L/etc.), PAPI (P2L/P4L/etc.), SAVASI (S2L/S2R), Tri-Color, Pulsating, Panel systems.</para>
    /// </summary>
    public VisualGlideSlopeIndicatorType? VisualGlideSlopeIndicator { get; init; }

    /// <summary>
    /// FAA NASR field: RWY_VISUAL_RANGE_EQUIP_CODE. Runway Visual Range (RVR) equipment location.
    /// <para>Possible values: Touchdown (T), Midfield (M), Rollout (R), None (N), TouchdownMidfield (TM), TouchdownRollout (TR), MidfieldRollout (MR), TouchdownMidfieldRollout (TMR).</para>
    /// </summary>
    public RunwayVisualRangeEquipmentType? RunwayVisualRangeEquipment { get; init; }

    /// <summary>
    /// FAA NASR field: RWY_VSBY_VALUE_EQUIP_FLAG. Whether Runway Visibility Value (RVV) equipment is installed.
    /// </summary>
    public bool RunwayVisibilityValueEquipment { get; init; }

    /// <summary>
    /// FAA NASR field: APCH_LGT_SYSTEM_CODE. Approach light system type.
    /// <para>See <see cref="ApproachLightSystemType"/> enum for all possible values and their FAA descriptions.</para>
    /// </summary>
    public ApproachLightSystemType? ApproachLightSystem { get; init; }

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
    public ControllingObjectMarking? ControllingObjectMarking { get; init; }

    /// <summary>FAA NASR field: OBSTN_CLNC_SLOPE. Controlling object clearance slope value, expressed as a ratio of N:1. If greater than 50:1, then 50 is entered.</summary>
    public int? ControllingObjectClearanceSlope { get; init; }

    /// <summary>FAA NASR field: OBSTN_HGT. Controlling object height above the physical runway end in feet AGL.</summary>
    public int? ControllingObjectHeightAboveRunway { get; init; }

    /// <summary>FAA NASR field: DIST_FROM_THR. Controlling object distance from the physical runway end in feet.</summary>
    public int? ControllingObjectDistanceFromRunway { get; init; }

    /// <summary>FAA NASR field: CNTRLN_OFFSET. Controlling object centerline offset distance from the extended runway centerline, in feet.</summary>
    public string? ControllingObjectCenterlineOffset { get; init; }

    // ── DMS Coordinates - Runway End ─────────────────────────────────────

    /// <summary>FAA NASR field: RWY_END_LAT_DEG. Runway end latitude degrees.</summary>
    public int? RwyEndLatDeg { get; init; }

    /// <summary>FAA NASR field: RWY_END_LAT_MIN. Runway end latitude minutes.</summary>
    public int? RwyEndLatMin { get; init; }

    /// <summary>FAA NASR field: RWY_END_LAT_SEC. Runway end latitude seconds.</summary>
    public double? RwyEndLatSec { get; init; }

    /// <summary>FAA NASR field: RWY_END_LAT_HEMIS. Runway end latitude hemisphere (N or S).</summary>
    public string? RwyEndLatHemis { get; init; }

    /// <summary>FAA NASR field: RWY_END_LONG_DEG. Runway end longitude degrees.</summary>
    public int? RwyEndLongDeg { get; init; }

    /// <summary>FAA NASR field: RWY_END_LONG_MIN. Runway end longitude minutes.</summary>
    public int? RwyEndLongMin { get; init; }

    /// <summary>FAA NASR field: RWY_END_LONG_SEC. Runway end longitude seconds.</summary>
    public double? RwyEndLongSec { get; init; }

    /// <summary>FAA NASR field: RWY_END_LONG_HEMIS. Runway end longitude hemisphere (E or W).</summary>
    public string? RwyEndLongHemis { get; init; }

    // ── DMS Coordinates - Displaced Threshold ────────────────────────────

    /// <summary>FAA NASR field: DISPLACED_THR_LAT_DEG. Displaced threshold latitude degrees.</summary>
    public int? DisplacedThrLatDeg { get; init; }

    /// <summary>FAA NASR field: DISPLACED_THR_LAT_MIN. Displaced threshold latitude minutes.</summary>
    public int? DisplacedThrLatMin { get; init; }

    /// <summary>FAA NASR field: DISPLACED_THR_LAT_SEC. Displaced threshold latitude seconds.</summary>
    public double? DisplacedThrLatSec { get; init; }

    /// <summary>FAA NASR field: DISPLACED_THR_LAT_HEMIS. Displaced threshold latitude hemisphere (N or S).</summary>
    public string? DisplacedThrLatHemis { get; init; }

    /// <summary>FAA NASR field: DISPLACED_THR_LONG_DEG. Displaced threshold longitude degrees.</summary>
    public int? DisplacedThrLongDeg { get; init; }

    /// <summary>FAA NASR field: DISPLACED_THR_LONG_MIN. Displaced threshold longitude minutes.</summary>
    public int? DisplacedThrLongMin { get; init; }

    /// <summary>FAA NASR field: DISPLACED_THR_LONG_SEC. Displaced threshold longitude seconds.</summary>
    public double? DisplacedThrLongSec { get; init; }

    /// <summary>FAA NASR field: DISPLACED_THR_LONG_HEMIS. Displaced threshold longitude hemisphere (E or W).</summary>
    public string? DisplacedThrLongHemis { get; init; }

    // ── Codes & Gradient ─────────────────────────────────────────────────

    /// <summary>
    /// FAA NASR field: FAR_PART_77_CODE. FAA CFR Part 77 (Objects Affecting Navigable Airspace) Runway Category.
    /// <para>Possible values: A(V) (Utility Runway with Visual Approach), B(V) (Other Than Utility with Visual Approach),
    /// A(NP) (Utility with Nonprecision Approach), C (Other Than Utility with Nonprecision, visibility &gt; 3/4 mile),
    /// D (Other Than Utility with Nonprecision, visibility as low as 3/4 mile), PIR (Precision Instrument Runway).</para>
    /// </summary>
    public string? FarPart77Code { get; init; }

    /// <summary>FAA NASR field: CNTRLN_DIR_CODE. Controlling Object Centerline Offset Direction. Indicates direction (left or right) to the object from the centerline as seen by an approaching pilot.</summary>
    public string? CenterlineDirectionCode { get; init; }

    /// <summary>FAA NASR field: RWY_GRAD. Runway End Gradient as a percentage (e.g., 0.3 = 0.3% grade).</summary>
    public double? RunwayGradient { get; init; }

    /// <summary>FAA NASR field: RWY_GRAD_DIRECTION. Runway End Gradient Direction (Up or Down).</summary>
    public string? RunwayGradientDirection { get; init; }

    // ── Source/Date Metadata ─────────────────────────────────────────────

    /// <summary>FAA NASR field: RWY_END_PSN_SOURCE. Source of runway end position information.</summary>
    public string? RwyEndPositionSource { get; init; }

    /// <summary>FAA NASR field: RWY_END_PSN_DATE. Date of runway end position information. ISO 8601 UTC format.</summary>
    public DateTime? RwyEndPositionDate { get; init; }

    /// <summary>FAA NASR field: RWY_END_ELEV_SOURCE. Source of runway end elevation information.</summary>
    public string? RwyEndElevationSource { get; init; }

    /// <summary>FAA NASR field: RWY_END_ELEV_DATE. Date of runway end elevation information. ISO 8601 UTC format.</summary>
    public DateTime? RwyEndElevationDate { get; init; }

    /// <summary>FAA NASR field: DSPL_THR_PSN_SOURCE. Source of displaced threshold position information.</summary>
    public string? DisplacedThrPositionSource { get; init; }

    /// <summary>FAA NASR field: RWY_END_DSPL_THR_PSN_DATE. Date of displaced threshold position information. ISO 8601 UTC format.</summary>
    public DateTime? DisplacedThrPositionDate { get; init; }

    /// <summary>FAA NASR field: DSPL_THR_ELEV_SOURCE. Source of displaced threshold elevation information.</summary>
    public string? DisplacedThrElevationSource { get; init; }

    /// <summary>FAA NASR field: RWY_END_DSPL_THR_ELEV_DATE. Date of displaced threshold elevation information. ISO 8601 UTC format.</summary>
    public DateTime? DisplacedThrElevationDate { get; init; }

    /// <summary>FAA NASR field: TDZ_ELEV_SOURCE. Source of touchdown zone elevation information.</summary>
    public string? TouchdownZoneElevSource { get; init; }

    /// <summary>FAA NASR field: RWY_END_TDZ_ELEV_DATE. Date of touchdown zone elevation information. ISO 8601 UTC format.</summary>
    public DateTime? TouchdownZoneElevDate { get; init; }

    // ── Declared Distances ───────────────────────────────────────────────

    /// <summary>FAA NASR field: TKOF_RUN_AVBL. Takeoff Run Available (TORA) in feet.</summary>
    public int? TakeoffRunAvailable { get; init; }

    /// <summary>FAA NASR field: TKOF_DIST_AVBL. Takeoff Distance Available (TODA) in feet.</summary>
    public int? TakeoffDistanceAvailable { get; init; }

    /// <summary>FAA NASR field: ACLT_STOP_DIST_AVBL. Accelerate-Stop Distance Available (ASDA) in feet.</summary>
    public int? AccelerateStopDistAvailable { get; init; }

    /// <summary>FAA NASR field: LNDG_DIST_AVBL. Landing Distance Available (LDA) in feet.</summary>
    public int? LandingDistanceAvailable { get; init; }

    // ── LAHSO (Land and Hold Short Operations) ───────────────────────────

    /// <summary>FAA NASR field: LAHSO_ALD. Available Landing Distance for Land and Hold Short Operations (LAHSO), in feet.</summary>
    public int? LahsoAvailableLandingDistance { get; init; }

    /// <summary>FAA NASR field: RWY_END_INTERSECT_LAHSO. ID of Intersecting Runway Defining Hold Short Point.</summary>
    public string? LahsoIntersectingRunway { get; init; }

    /// <summary>FAA NASR field: LAHSO_DESC. Description of Entity Defining Hold Short Point if not an Intersecting Runway.</summary>
    public string? LahsoDescription { get; init; }

    /// <summary>FAA NASR field: LAHSO_LAT. LAHSO hold short point latitude (DMS format).</summary>
    public string? LahsoLatitude { get; init; }

    /// <summary>FAA NASR field: LAT_LAHSO_DECIMAL. LAHSO hold short point latitude in decimal degrees (WGS 84).</summary>
    public double? LahsoLatDecimal { get; init; }

    /// <summary>FAA NASR field: LAHSO_LONG. LAHSO hold short point longitude (DMS format).</summary>
    public string? LahsoLongitude { get; init; }

    /// <summary>FAA NASR field: LONG_LAHSO_DECIMAL. LAHSO hold short point longitude in decimal degrees (WGS 84).</summary>
    public double? LahsoLongDecimal { get; init; }

    /// <summary>FAA NASR field: LAHSO_PSN_SOURCE. Source of LAHSO position information.</summary>
    public string? LahsoPositionSource { get; init; }

    /// <summary>FAA NASR field: RWY_END_LAHSO_PSN_DATE. Date of LAHSO position information. ISO 8601 UTC format.</summary>
    public DateTime? LahsoPositionDate { get; init; }
}

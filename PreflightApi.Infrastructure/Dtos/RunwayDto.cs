using PreflightApi.Domain.Enums;

namespace PreflightApi.Infrastructure.Dtos;

/// <summary>
/// Runway data including dimensions, surface, and lighting information.
/// </summary>
public record RunwayDto
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; init; }
    /// <summary>Runway identifier (e.g., "17L/35R").</summary>
    public string RunwayId { get; init; } = string.Empty;
    /// <summary>Runway length in feet.</summary>
    public int? Length { get; init; }
    /// <summary>Runway width in feet.</summary>
    public int? Width { get; init; }
    /// <summary>Surface type (e.g., asphalt, concrete, turf).</summary>
    public RunwaySurfaceType SurfaceType { get; init; }
    /// <summary>Surface treatment (e.g., grooved, porous friction).</summary>
    public RunwaySurfaceTreatment SurfaceTreatment { get; init; }
    /// <summary>Pavement classification number.</summary>
    public string? PavementClassification { get; init; }
    /// <summary>Edge light intensity (e.g., high, medium, low).</summary>
    public RunwayEdgeLightIntensity EdgeLightIntensity { get; init; }
    /// <summary>Single wheel weight bearing capacity in pounds.</summary>
    public int? WeightBearingSingleWheel { get; init; }
    /// <summary>Dual wheel weight bearing capacity in pounds.</summary>
    public int? WeightBearingDualWheel { get; init; }
    /// <summary>Dual tandem weight bearing capacity in pounds.</summary>
    public int? WeightBearingDualTandem { get; init; }
    /// <summary>Double dual tandem weight bearing capacity in pounds.</summary>
    public int? WeightBearingDoubleDualTandem { get; init; }
    /// <summary>Runway end details for each direction.</summary>
    public List<RunwayEndDto> RunwayEnds { get; init; } = new();
}

/// <summary>
/// Runway end data including approach, markings, lighting, and obstacle information.
/// </summary>
public record RunwayEndDto
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; init; }
    /// <summary>Runway end identifier (e.g., "17L").</summary>
    public string RunwayEndId { get; init; } = string.Empty;
    /// <summary>True alignment heading in degrees.</summary>
    public int? TrueAlignment { get; init; }
    /// <summary>Type of instrument approach available.</summary>
    public InstrumentApproachType ApproachType { get; init; }
    /// <summary>Whether right-hand traffic pattern is in effect.</summary>
    public bool RightHandTrafficPattern { get; init; }
    /// <summary>Type of runway markings.</summary>
    public RunwayMarkingsType MarkingsType { get; init; }
    /// <summary>Condition of the runway markings.</summary>
    public RunwayMarkingsCondition MarkingsCondition { get; init; }
    /// <summary>Latitude of the runway end in decimal degrees.</summary>
    public decimal? Latitude { get; init; }
    /// <summary>Longitude of the runway end in decimal degrees.</summary>
    public decimal? Longitude { get; init; }
    /// <summary>Elevation of the runway end in feet MSL.</summary>
    public decimal? Elevation { get; init; }
    /// <summary>Threshold crossing height in feet AGL.</summary>
    public decimal? ThresholdCrossingHeight { get; init; }
    /// <summary>Visual glide path angle in degrees.</summary>
    public decimal? VisualGlidePathAngle { get; init; }
    /// <summary>Displaced threshold latitude in decimal degrees.</summary>
    public decimal? DisplacedThresholdLatitude { get; init; }
    /// <summary>Displaced threshold longitude in decimal degrees.</summary>
    public decimal? DisplacedThresholdLongitude { get; init; }
    /// <summary>Displaced threshold elevation in feet MSL.</summary>
    public decimal? DisplacedThresholdElevation { get; init; }
    /// <summary>Displaced threshold length in feet.</summary>
    public int? DisplacedThresholdLength { get; init; }
    /// <summary>Touchdown zone elevation in feet MSL.</summary>
    public decimal? TouchdownZoneElevation { get; init; }
    /// <summary>Type of visual glide slope indicator (e.g., VASI, PAPI).</summary>
    public VisualGlideSlopeIndicatorType VisualGlideSlopeIndicator { get; init; }
    /// <summary>Runway visual range equipment type.</summary>
    public string? RunwayVisualRangeEquipment { get; init; }
    /// <summary>Whether runway visibility value equipment is installed.</summary>
    public bool RunwayVisibilityValueEquipment { get; init; }
    /// <summary>Type of approach light system.</summary>
    public ApproachLightSystemType ApproachLightSystem { get; init; }
    /// <summary>Whether runway end identifier lights are installed.</summary>
    public bool HasRunwayEndLights { get; init; }
    /// <summary>Whether centerline lights are installed.</summary>
    public bool HasCenterlineLights { get; init; }
    /// <summary>Whether touchdown zone lights are installed.</summary>
    public bool HasTouchdownZoneLights { get; init; }
    /// <summary>Description of the controlling obstacle.</summary>
    public string? ControllingObjectDescription { get; init; }
    /// <summary>Marking type of the controlling obstacle.</summary>
    public ControllingObjectMarking ControllingObjectMarking { get; init; }
    /// <summary>Clearance slope of the controlling obstacle.</summary>
    public int? ControllingObjectClearanceSlope { get; init; }
    /// <summary>Height of the controlling obstacle above the runway in feet.</summary>
    public int? ControllingObjectHeightAboveRunway { get; init; }
    /// <summary>Distance of the controlling obstacle from the runway end in feet.</summary>
    public int? ControllingObjectDistanceFromRunway { get; init; }
    /// <summary>Centerline offset of the controlling obstacle.</summary>
    public string? ControllingObjectCenterlineOffset { get; init; }
}

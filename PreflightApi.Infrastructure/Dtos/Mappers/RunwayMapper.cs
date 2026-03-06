using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.Dtos.Mappers;

public static class RunwayMapper
{
    private static readonly Dictionary<string, RunwaySurfaceType> SurfaceTypeMapping = new()
    {
        ["CONC"] = RunwaySurfaceType.Concrete,
        ["ASPH"] = RunwaySurfaceType.Asphalt,
        ["SNOW"] = RunwaySurfaceType.Snow,
        ["ICE"] = RunwaySurfaceType.Ice,
        ["MATS"] = RunwaySurfaceType.Mats,
        ["TREATED"] = RunwaySurfaceType.Treated,
        ["TRTD"] = RunwaySurfaceType.Treated,
        ["GRAVEL"] = RunwaySurfaceType.Gravel,
        ["GRVL"] = RunwaySurfaceType.Gravel,
        ["TURF"] = RunwaySurfaceType.Turf,
        ["DIRT"] = RunwaySurfaceType.Dirt,
        ["PEM"] = RunwaySurfaceType.PartiallyPaved,
        ["ROOF-TOP"] = RunwaySurfaceType.Rooftop,
        ["ROOFTOP"] = RunwaySurfaceType.Rooftop,
        ["ROOF"] = RunwaySurfaceType.Rooftop,
        ["WATER"] = RunwaySurfaceType.Water,
        ["ALUMINUM"] = RunwaySurfaceType.Aluminum,
        ["ALUM"] = RunwaySurfaceType.Aluminum,
        ["BRICK"] = RunwaySurfaceType.Brick,
        ["CALICHE"] = RunwaySurfaceType.Caliche,
        ["CORAL"] = RunwaySurfaceType.Coral,
        ["DECK"] = RunwaySurfaceType.Deck,
        ["GRASS"] = RunwaySurfaceType.Grass,
        ["METAL"] = RunwaySurfaceType.Metal,
        ["NSTD"] = RunwaySurfaceType.NonStandard,
        ["OR"] = RunwaySurfaceType.NonStandard,
        ["OIL&CHIP"] = RunwaySurfaceType.OilChip,
        ["PSP"] = RunwaySurfaceType.Psp,
        ["SAND"] = RunwaySurfaceType.Sand,
        ["SOD"] = RunwaySurfaceType.Sod,
        ["STEEL"] = RunwaySurfaceType.Steel,
        ["WOOD"] = RunwaySurfaceType.Wood,
        ["PFC"] = RunwaySurfaceType.PorousFrictionCourse
    };

    private static readonly Dictionary<string, RunwaySurfaceTreatment> SurfaceTreatmentMapping = new()
    {
        ["NONE"] = RunwaySurfaceTreatment.None,
        ["GRVD"] = RunwaySurfaceTreatment.Grooved,
        ["PFC"] = RunwaySurfaceTreatment.PorousFrictionCourse,
        ["AFSC"] = RunwaySurfaceTreatment.AggregateFrictionSealCoat,
        ["RFSC"] = RunwaySurfaceTreatment.RubberizedFrictionSealCoat,
        ["WC"] = RunwaySurfaceTreatment.WireComb
    };

    private static readonly Dictionary<string, RunwayEdgeLightIntensity> EdgeLightIntensityMapping = new()
    {
        ["NONE"] = RunwayEdgeLightIntensity.None,
        ["HIGH"] = RunwayEdgeLightIntensity.High,
        ["MED"] = RunwayEdgeLightIntensity.Medium,
        ["LOW"] = RunwayEdgeLightIntensity.Low,
        ["FLD"] = RunwayEdgeLightIntensity.Flood,
        ["NSTD"] = RunwayEdgeLightIntensity.NonStandard,
        ["PERI"] = RunwayEdgeLightIntensity.Perimeter,
        ["STRB"] = RunwayEdgeLightIntensity.Strobe
    };

    private static readonly Dictionary<string, InstrumentApproachType> ApproachTypeMapping = new()
    {
        ["ILS"] = InstrumentApproachType.Ils,
        ["MLS"] = InstrumentApproachType.Mls,
        ["SDF"] = InstrumentApproachType.Sdf,
        ["LOCALIZER"] = InstrumentApproachType.Localizer,
        ["LDA"] = InstrumentApproachType.Lda,
        ["ISMLS"] = InstrumentApproachType.Ismls,
        ["ILS/DME"] = InstrumentApproachType.IlsDme,
        ["SDF/DME"] = InstrumentApproachType.SdfDme,
        ["LOC/DME"] = InstrumentApproachType.LocDme,
        ["LOC/GS"] = InstrumentApproachType.LocGs,
        ["LDA/DME"] = InstrumentApproachType.LdaDme
    };

    private static readonly Dictionary<string, RunwayMarkingsType> MarkingsTypeMapping = new()
    {
        ["NONE"] = RunwayMarkingsType.None,
        ["PIR"] = RunwayMarkingsType.PrecisionInstrument,
        ["NPI"] = RunwayMarkingsType.NonPrecisionInstrument,
        ["BSC"] = RunwayMarkingsType.Basic,
        ["NRS"] = RunwayMarkingsType.NumbersOnly,
        ["NSTD"] = RunwayMarkingsType.NonStandard,
        ["BUOY"] = RunwayMarkingsType.Buoys,
        ["STOL"] = RunwayMarkingsType.Stol
    };

    private static readonly Dictionary<string, RunwayMarkingsCondition> MarkingsConditionMapping = new()
    {
        ["G"] = RunwayMarkingsCondition.Good,
        ["F"] = RunwayMarkingsCondition.Fair,
        ["P"] = RunwayMarkingsCondition.Poor,
        ["GOOD"] = RunwayMarkingsCondition.Good,
        ["FAIR"] = RunwayMarkingsCondition.Fair,
        ["POOR"] = RunwayMarkingsCondition.Poor
    };

    private static readonly Dictionary<string, RunwaySurfaceCondition> SurfaceConditionMapping = new()
    {
        ["EXCELLENT"] = RunwaySurfaceCondition.Excellent,
        ["GOOD"] = RunwaySurfaceCondition.Good,
        ["FAIR"] = RunwaySurfaceCondition.Fair,
        ["POOR"] = RunwaySurfaceCondition.Poor,
        ["FAILED"] = RunwaySurfaceCondition.Failed
    };

    private static readonly Dictionary<string, PavementType> PavementTypeMapping = new()
    {
        ["R"] = PavementType.Rigid,
        ["F"] = PavementType.Flexible
    };

    private static readonly Dictionary<string, SubgradeStrength> SubgradeStrengthMapping = new()
    {
        ["A"] = SubgradeStrength.High,
        ["B"] = SubgradeStrength.Medium,
        ["C"] = SubgradeStrength.Low,
        ["D"] = SubgradeStrength.UltraLow
    };

    private static readonly Dictionary<string, TirePressure> TirePressureMapping = new()
    {
        ["W"] = TirePressure.High,
        ["X"] = TirePressure.Medium,
        ["Y"] = TirePressure.Low,
        ["Z"] = TirePressure.VeryLow
    };

    private static readonly Dictionary<string, PavementDeterminationMethod> DeterminationMethodMapping = new()
    {
        ["T"] = PavementDeterminationMethod.Technical,
        ["U"] = PavementDeterminationMethod.UsingAircraft
    };

    private static readonly Dictionary<string, VisualGlideSlopeIndicatorType> VisualGlideSlopeIndicatorMapping = new()
    {
        ["NONE"] = VisualGlideSlopeIndicatorType.None,
        ["N"] = VisualGlideSlopeIndicatorType.None,
        // SAVASI
        ["S2L"] = VisualGlideSlopeIndicatorType.Savasi2BoxLeft,
        ["S2R"] = VisualGlideSlopeIndicatorType.Savasi2BoxRight,
        // VASI
        ["V2L"] = VisualGlideSlopeIndicatorType.Vasi2BoxLeft,
        ["V2R"] = VisualGlideSlopeIndicatorType.Vasi2BoxRight,
        ["V4L"] = VisualGlideSlopeIndicatorType.Vasi4BoxLeft,
        ["V4R"] = VisualGlideSlopeIndicatorType.Vasi4BoxRight,
        ["V6L"] = VisualGlideSlopeIndicatorType.Vasi6BoxLeft,
        ["V6R"] = VisualGlideSlopeIndicatorType.Vasi6BoxRight,
        ["V12"] = VisualGlideSlopeIndicatorType.Vasi12Box,
        ["V16"] = VisualGlideSlopeIndicatorType.Vasi16Box,
        // PAPI
        ["P2L"] = VisualGlideSlopeIndicatorType.Papi2LightLeft,
        ["P2R"] = VisualGlideSlopeIndicatorType.Papi2LightRight,
        ["P4L"] = VisualGlideSlopeIndicatorType.Papi4LightLeft,
        ["P4R"] = VisualGlideSlopeIndicatorType.Papi4LightRight,
        // Tri-Color
        ["TRIL"] = VisualGlideSlopeIndicatorType.TriColorLeft,
        ["TRIR"] = VisualGlideSlopeIndicatorType.TriColorRight,
        // Pulsating
        ["PSIL"] = VisualGlideSlopeIndicatorType.PulsatingLeft,
        ["PSIR"] = VisualGlideSlopeIndicatorType.PulsatingRight,
        // Panel
        ["PNIL"] = VisualGlideSlopeIndicatorType.PanelLeft,
        ["PNIR"] = VisualGlideSlopeIndicatorType.PanelRight,
        // Other
        ["NSTD"] = VisualGlideSlopeIndicatorType.NonStandard,
        ["PVT"] = VisualGlideSlopeIndicatorType.PrivateUse,
        ["VAS"] = VisualGlideSlopeIndicatorType.NonSpecificVasi
    };

    private static readonly Dictionary<string, ApproachLightSystemType> ApproachLightSystemMapping = new()
    {
        ["NONE"] = ApproachLightSystemType.None,
        ["AFOVRN"] = ApproachLightSystemType.AirForceOverrun,
        ["ALSAF"] = ApproachLightSystemType.Alsaf,
        ["ALSF1"] = ApproachLightSystemType.Alsf1,
        ["ALSF2"] = ApproachLightSystemType.Alsf2,
        ["MALS"] = ApproachLightSystemType.Mals,
        ["MALSF"] = ApproachLightSystemType.Malsf,
        ["MALSR"] = ApproachLightSystemType.Malsr,
        ["RAIL"] = ApproachLightSystemType.Rail,
        ["SALS"] = ApproachLightSystemType.Sals,
        ["SALSF"] = ApproachLightSystemType.Salsf,
        ["SSALS"] = ApproachLightSystemType.Ssals,
        ["SSALF"] = ApproachLightSystemType.Ssalf,
        ["SSALR"] = ApproachLightSystemType.Ssalr,
        ["ODALS"] = ApproachLightSystemType.Odals,
        ["RLLS"] = ApproachLightSystemType.Rlls,
        ["MIL OVRN"] = ApproachLightSystemType.MilitaryOverrun,
        ["NSTD"] = ApproachLightSystemType.NonStandard
    };

    private static readonly Dictionary<string, ControllingObjectMarking> ControllingObjectMarkingMapping = new()
    {
        ["NONE"] = ControllingObjectMarking.None,
        ["M"] = ControllingObjectMarking.Marked,
        ["L"] = ControllingObjectMarking.Lighted,
        ["ML"] = ControllingObjectMarking.MarkedAndLighted,
        ["LM"] = ControllingObjectMarking.MarkedAndLighted
    };

    private static readonly Dictionary<string, RunwayVisualRangeEquipmentType> RunwayVisualRangeEquipmentMapping = new()
    {
        ["N"] = RunwayVisualRangeEquipmentType.None,
        ["T"] = RunwayVisualRangeEquipmentType.Touchdown,
        ["M"] = RunwayVisualRangeEquipmentType.Midfield,
        ["R"] = RunwayVisualRangeEquipmentType.Rollout,
        ["TM"] = RunwayVisualRangeEquipmentType.TouchdownMidfield,
        ["TR"] = RunwayVisualRangeEquipmentType.TouchdownRollout,
        ["MR"] = RunwayVisualRangeEquipmentType.MidfieldRollout,
        ["TMR"] = RunwayVisualRangeEquipmentType.TouchdownMidfieldRollout
    };

    public static RunwayDto ToDto(Runway runway, Airport airport, ILogger logger, bool includeGeometry = false)
    {
        var runwayId = runway.RunwayId ?? runway.Id.ToString();
        var (primarySurface, secondarySurface) = ParseSurfaceType(runway.SurfaceTypeCode, logger, runwayId);

        return new RunwayDto
        {
            AirportIcaoCode = airport.IcaoId,
            AirportArptId = airport.ArptId,
            AirportName = airport.ArptName,
            Id = runway.Id,
            RunwayId = runway.RunwayId,
            Length = runway.Length,
            Width = runway.Width,
            SurfaceType = primarySurface,
            SecondarySurfaceType = secondarySurface,
            SurfaceTreatment = EnumParseHelper.Parse(runway.SurfaceTreatmentCode, logger, "SurfaceTreatment", "Runway", runwayId, SurfaceTreatmentMapping),
            PavementClassification = runway.PavementClassification,
            EdgeLightIntensity = EnumParseHelper.Parse(runway.EdgeLightIntensity, logger, "EdgeLightIntensity", "Runway", runwayId, EdgeLightIntensityMapping),
            WeightBearingSingleWheel = runway.WeightBearingSingleWheel,
            WeightBearingDualWheel = runway.WeightBearingDualWheel,
            WeightBearingDualTandem = runway.WeightBearingDualTandem,
            WeightBearingDoubleDualTandem = runway.WeightBearingDoubleDualTandem,
            SurfaceCondition = EnumParseHelper.Parse(runway.SurfaceCondition, logger, "SurfaceCondition", "Runway", runwayId, SurfaceConditionMapping),
            PavementType = EnumParseHelper.Parse(runway.PavementTypeCode, logger, "PavementType", "Runway", runwayId, PavementTypeMapping),
            SubgradeStrength = EnumParseHelper.Parse(runway.SubgradeStrengthCode, logger, "SubgradeStrength", "Runway", runwayId, SubgradeStrengthMapping),
            TirePressure = EnumParseHelper.Parse(runway.TirePressureCode, logger, "TirePressure", "Runway", runwayId, TirePressureMapping),
            DeterminationMethod = EnumParseHelper.Parse(runway.DeterminationMethodCode, logger, "DeterminationMethod", "Runway", runwayId, DeterminationMethodMapping),
            RunwayLengthSource = runway.RunwayLengthSource,
            LengthSourceDate = runway.LengthSourceDate,
            Geometry = includeGeometry && runway.Geometry != null ? ConvertToGeoJson(runway.Geometry) : null,
            RunwayEnds = runway.RunwayEnds?.Select(re => ToDto(re, logger)).ToList() ?? new List<RunwayEndDto>()
        };
    }

    public static RunwayDto ToDto(Runway runway, ILogger logger)
    {
        var runwayId = runway.RunwayId ?? runway.Id.ToString();
        var (primarySurface, secondarySurface) = ParseSurfaceType(runway.SurfaceTypeCode, logger, runwayId);

        return new RunwayDto
        {
            Id = runway.Id,
            RunwayId = runway.RunwayId,
            Length = runway.Length,
            Width = runway.Width,
            SurfaceType = primarySurface,
            SecondarySurfaceType = secondarySurface,
            SurfaceTreatment = EnumParseHelper.Parse(runway.SurfaceTreatmentCode, logger, "SurfaceTreatment", "Runway", runwayId, SurfaceTreatmentMapping),
            PavementClassification = runway.PavementClassification,
            EdgeLightIntensity = EnumParseHelper.Parse(runway.EdgeLightIntensity, logger, "EdgeLightIntensity", "Runway", runwayId, EdgeLightIntensityMapping),
            WeightBearingSingleWheel = runway.WeightBearingSingleWheel,
            WeightBearingDualWheel = runway.WeightBearingDualWheel,
            WeightBearingDualTandem = runway.WeightBearingDualTandem,
            WeightBearingDoubleDualTandem = runway.WeightBearingDoubleDualTandem,
            SurfaceCondition = EnumParseHelper.Parse(runway.SurfaceCondition, logger, "SurfaceCondition", "Runway", runwayId, SurfaceConditionMapping),
            PavementType = EnumParseHelper.Parse(runway.PavementTypeCode, logger, "PavementType", "Runway", runwayId, PavementTypeMapping),
            SubgradeStrength = EnumParseHelper.Parse(runway.SubgradeStrengthCode, logger, "SubgradeStrength", "Runway", runwayId, SubgradeStrengthMapping),
            TirePressure = EnumParseHelper.Parse(runway.TirePressureCode, logger, "TirePressure", "Runway", runwayId, TirePressureMapping),
            DeterminationMethod = EnumParseHelper.Parse(runway.DeterminationMethodCode, logger, "DeterminationMethod", "Runway", runwayId, DeterminationMethodMapping),
            RunwayLengthSource = runway.RunwayLengthSource,
            LengthSourceDate = runway.LengthSourceDate,
            RunwayEnds = runway.RunwayEnds?.Select(re => ToDto(re, logger)).ToList() ?? new List<RunwayEndDto>()
        };
    }

    public static RunwayEndDto ToDto(RunwayEnd runwayEnd, ILogger logger)
    {
        var endId = runwayEnd.RunwayEndId ?? runwayEnd.Id.ToString();

        return new RunwayEndDto
        {
            Id = runwayEnd.Id,
            RunwayEndId = runwayEnd.RunwayEndId,
            TrueAlignment = runwayEnd.TrueAlignment,
            ApproachType = EnumParseHelper.Parse(runwayEnd.ApproachType, logger, "ApproachType", "RunwayEnd", endId, ApproachTypeMapping),
            RightHandTrafficPattern = runwayEnd.RightHandTrafficPattern,
            MarkingsType = EnumParseHelper.Parse(runwayEnd.RunwayMarkingsType, logger, "MarkingsType", "RunwayEnd", endId, MarkingsTypeMapping),
            MarkingsCondition = EnumParseHelper.Parse(runwayEnd.RunwayMarkingsCondition, logger, "MarkingsCondition", "RunwayEnd", endId, MarkingsConditionMapping),
            Latitude = runwayEnd.LatDecimal,
            Longitude = runwayEnd.LongDecimal,
            Elevation = runwayEnd.Elevation,
            ThresholdCrossingHeight = runwayEnd.ThresholdCrossingHeight,
            VisualGlidePathAngle = runwayEnd.VisualGlidePathAngle,
            DisplacedThresholdLatitude = runwayEnd.DisplacedThresholdLatDecimal,
            DisplacedThresholdLongitude = runwayEnd.DisplacedThresholdLongDecimal,
            DisplacedThresholdElevation = runwayEnd.DisplacedThresholdElev,
            DisplacedThresholdLength = runwayEnd.DisplacedThresholdLength,
            TouchdownZoneElevation = runwayEnd.TouchdownZoneElev,
            VisualGlideSlopeIndicator = EnumParseHelper.Parse(runwayEnd.VisualGlideSlopeIndicator, logger, "VisualGlideSlopeIndicator", "RunwayEnd", endId, VisualGlideSlopeIndicatorMapping),
            RunwayVisualRangeEquipment = EnumParseHelper.Parse(runwayEnd.RunwayVisualRangeEquipment, logger, "RunwayVisualRangeEquipment", "RunwayEnd", endId, RunwayVisualRangeEquipmentMapping),
            RunwayVisibilityValueEquipment = runwayEnd.RunwayVisibilityValueEquipment,
            ApproachLightSystem = EnumParseHelper.Parse(runwayEnd.ApproachLightSystem, logger, "ApproachLightSystem", "RunwayEnd", endId, ApproachLightSystemMapping),
            HasRunwayEndLights = runwayEnd.RunwayEndLights,
            HasCenterlineLights = runwayEnd.CenterlineLights,
            HasTouchdownZoneLights = runwayEnd.TouchdownZoneLights,
            ControllingObjectDescription = runwayEnd.ControllingObjectDescription,
            ControllingObjectMarking = EnumParseHelper.Parse(runwayEnd.ControllingObjectMarkedLighted, logger, "ControllingObjectMarking", "RunwayEnd", endId, ControllingObjectMarkingMapping),
            ControllingObjectClearanceSlope = runwayEnd.ControllingObjectClearanceSlope,
            ControllingObjectHeightAboveRunway = runwayEnd.ControllingObjectHeightAboveRunway,
            ControllingObjectDistanceFromRunway = runwayEnd.ControllingObjectDistanceFromRunway,
            ControllingObjectCenterlineOffset = runwayEnd.ControllingObjectCenterlineOffset,

            // DMS Coordinates - Runway End
            RwyEndLatDeg = runwayEnd.RwyEndLatDeg,
            RwyEndLatMin = runwayEnd.RwyEndLatMin,
            RwyEndLatSec = runwayEnd.RwyEndLatSec,
            RwyEndLatHemis = runwayEnd.RwyEndLatHemis,
            RwyEndLongDeg = runwayEnd.RwyEndLongDeg,
            RwyEndLongMin = runwayEnd.RwyEndLongMin,
            RwyEndLongSec = runwayEnd.RwyEndLongSec,
            RwyEndLongHemis = runwayEnd.RwyEndLongHemis,

            // DMS Coordinates - Displaced Threshold
            DisplacedThrLatDeg = runwayEnd.DisplacedThrLatDeg,
            DisplacedThrLatMin = runwayEnd.DisplacedThrLatMin,
            DisplacedThrLatSec = runwayEnd.DisplacedThrLatSec,
            DisplacedThrLatHemis = runwayEnd.DisplacedThrLatHemis,
            DisplacedThrLongDeg = runwayEnd.DisplacedThrLongDeg,
            DisplacedThrLongMin = runwayEnd.DisplacedThrLongMin,
            DisplacedThrLongSec = runwayEnd.DisplacedThrLongSec,
            DisplacedThrLongHemis = runwayEnd.DisplacedThrLongHemis,

            // Codes & Gradient
            FarPart77Code = runwayEnd.FarPart77Code,
            CenterlineDirectionCode = runwayEnd.CenterlineDirectionCode,
            RunwayGradient = runwayEnd.RunwayGradient,
            RunwayGradientDirection = runwayEnd.RunwayGradientDirection,

            // Source/Date Metadata
            RwyEndPositionSource = runwayEnd.RwyEndPositionSource,
            RwyEndPositionDate = runwayEnd.RwyEndPositionDate,
            RwyEndElevationSource = runwayEnd.RwyEndElevationSource,
            RwyEndElevationDate = runwayEnd.RwyEndElevationDate,
            DisplacedThrPositionSource = runwayEnd.DisplacedThrPositionSource,
            DisplacedThrPositionDate = runwayEnd.DisplacedThrPositionDate,
            DisplacedThrElevationSource = runwayEnd.DisplacedThrElevationSource,
            DisplacedThrElevationDate = runwayEnd.DisplacedThrElevationDate,
            TouchdownZoneElevSource = runwayEnd.TouchdownZoneElevSource,
            TouchdownZoneElevDate = runwayEnd.TouchdownZoneElevDate,

            // Declared Distances
            TakeoffRunAvailable = runwayEnd.TakeoffRunAvailable,
            TakeoffDistanceAvailable = runwayEnd.TakeoffDistanceAvailable,
            AccelerateStopDistAvailable = runwayEnd.AccelerateStopDistAvailable,
            LandingDistanceAvailable = runwayEnd.LandingDistanceAvailable,

            // LAHSO
            LahsoAvailableLandingDistance = runwayEnd.LahsoAvailableLandingDistance,
            LahsoIntersectingRunway = runwayEnd.LahsoIntersectingRunway,
            LahsoDescription = runwayEnd.LahsoDescription,
            LahsoLatitude = runwayEnd.LahsoLatitude,
            LahsoLatDecimal = runwayEnd.LahsoLatDecimal,
            LahsoLongitude = runwayEnd.LahsoLongitude,
            LahsoLongDecimal = runwayEnd.LahsoLongDecimal,
            LahsoPositionSource = runwayEnd.LahsoPositionSource,
            LahsoPositionDate = runwayEnd.LahsoPositionDate
        };
    }

    /// <summary>
    /// Parses surface type codes, handling composite codes (e.g., "ASPH-CONC") by returning both primary and secondary types.
    /// </summary>
    private static (RunwaySurfaceType? Primary, RunwaySurfaceType? Secondary) ParseSurfaceType(string? code, ILogger logger, string runwayId)
    {
        if (string.IsNullOrWhiteSpace(code))
            return (null, null);

        var parts = code.Split('-', '/');
        var primary = EnumParseHelper.Parse(parts[0].Trim().ToUpperInvariant(), logger, "SurfaceType", "Runway", runwayId, SurfaceTypeMapping);

        if (parts.Length > 2)
            logger.LogWarning("SurfaceTypeCode '{Code}' for Runway {RunwayId} contains more than two components; only the first two will be mapped.", code, runwayId);

        RunwaySurfaceType? secondary = null;
        if (parts.Length > 1)
        {
            var secondaryCode = parts[1].Trim().ToUpperInvariant();
            if (!string.IsNullOrEmpty(secondaryCode))
                secondary = EnumParseHelper.Parse(secondaryCode, logger, "SurfaceType", "Runway", runwayId, SurfaceTypeMapping);
        }

        return (primary, secondary);
    }

    /// <summary>
    /// Converts a RunwaySurfaceType enum value back to the FAA database code for filtering.
    /// </summary>
    public static string? ToDbCode(RunwaySurfaceType surfaceType)
    {
        return surfaceType switch
        {
            RunwaySurfaceType.Concrete => "CONC",
            RunwaySurfaceType.Asphalt => "ASPH",
            RunwaySurfaceType.Snow => "SNOW",
            RunwaySurfaceType.Ice => "ICE",
            RunwaySurfaceType.Mats => "MATS",
            RunwaySurfaceType.Treated => "TREATED",
            RunwaySurfaceType.Gravel => "GRAVEL",
            RunwaySurfaceType.Turf => "TURF",
            RunwaySurfaceType.Dirt => "DIRT",
            RunwaySurfaceType.PartiallyPaved => "PEM",
            RunwaySurfaceType.Rooftop => "ROOF-TOP",
            RunwaySurfaceType.Water => "WATER",
            RunwaySurfaceType.Aluminum => "ALUMINUM",
            RunwaySurfaceType.Brick => "BRICK",
            RunwaySurfaceType.Caliche => "CALICHE",
            RunwaySurfaceType.Coral => "CORAL",
            RunwaySurfaceType.Deck => "DECK",
            RunwaySurfaceType.Grass => "GRASS",
            RunwaySurfaceType.Metal => "METAL",
            RunwaySurfaceType.NonStandard => "NSTD",
            RunwaySurfaceType.OilChip => "OIL&CHIP",
            RunwaySurfaceType.Psp => "PSP",
            RunwaySurfaceType.Sand => "SAND",
            RunwaySurfaceType.Sod => "SOD",
            RunwaySurfaceType.Steel => "STEEL",
            RunwaySurfaceType.Wood => "WOOD",
            RunwaySurfaceType.PorousFrictionCourse => "PFC",
            _ => null
        };
    }

    private static GeoJsonGeometry ConvertToGeoJson(Geometry geometry)
    {
        if (geometry is not Polygon polygon)
        {
            return new GeoJsonGeometry
            {
                Type = geometry.GeometryType,
                Coordinates = []
            };
        }

        var exteriorRing = polygon.ExteriorRing.Coordinates
            .Select(c => new[] { c.X, c.Y })
            .ToArray();

        var interiorRings = polygon.InteriorRings.Select(ring =>
            ring.Coordinates.Select(c => new[] { c.X, c.Y }).ToArray()
        ).ToArray();

        var allRings = new[] { exteriorRing }.Concat(interiorRings).ToArray();

        return new GeoJsonGeometry
        {
            Type = "Polygon",
            Coordinates = allRings
        };
    }
}

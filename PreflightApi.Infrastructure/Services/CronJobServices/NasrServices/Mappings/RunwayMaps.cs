using CsvHelper.Configuration;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Services.CronJobServices.NasrServices.Utils;

namespace PreflightApi.Infrastructure.Services.CronJobServices.NasrServices.Mappings;

public sealed class RunwayMap : ClassMap<Runway>
{
    public RunwayMap()
    {
        Map(m => m.SiteNo).Name("SITE_NO");
        Map(m => m.RunwayId).Name("RWY_ID");
        Map(m => m.Length).Name("RWY_LEN").TypeConverter<OptionalIntConverter>().Optional();
        Map(m => m.Width).Name("RWY_WIDTH").TypeConverter<OptionalIntConverter>().Optional();
        Map(m => m.SurfaceTypeCode).Name("SURFACE_TYPE_CODE").Optional();
        Map(m => m.SurfaceTreatmentCode).Name("TREATMENT_CODE").Optional();
        Map(m => m.PavementClassification).Name("PCN").Optional();
        Map(m => m.EdgeLightIntensity).Name("RWY_LGT_CODE").Optional();
        Map(m => m.WeightBearingSingleWheel).Name("GROSS_WT_SW").TypeConverter<OptionalIntConverter>().Optional();
        Map(m => m.WeightBearingDualWheel).Name("GROSS_WT_DW").TypeConverter<OptionalIntConverter>().Optional();
        Map(m => m.WeightBearingDualTandem).Name("GROSS_WT_DTW").TypeConverter<OptionalIntConverter>().Optional();
        Map(m => m.WeightBearingDoubleDualTandem).Name("GROSS_WT_DDTW").TypeConverter<OptionalIntConverter>().Optional();
        Map(m => m.SurfaceCondition).Name("COND").Optional();
        Map(m => m.PavementTypeCode).Name("PAVEMENT_TYPE_CODE").Optional();
        Map(m => m.SubgradeStrengthCode).Name("SUBGRADE_STRENGTH_CODE").Optional();
        Map(m => m.TirePressureCode).Name("TIRE_PRES_CODE").Optional();
        Map(m => m.DeterminationMethodCode).Name("DTRM_METHOD_CODE").Optional();
        Map(m => m.RunwayLengthSource).Name("RWY_LEN_SOURCE").Optional();
        Map(m => m.LengthSourceDate).Name("LENGTH_SOURCE_DATE").TypeConverter<OptionalDateConverter>().Optional();
    }
}

public sealed class RunwayEndMap : ClassMap<RunwayEnd>
{
    public RunwayEndMap()
    {
        Map(m => m.SiteNo).Name("SITE_NO");
        Map(m => m.RunwayIdRef).Name("RWY_ID");
        Map(m => m.RunwayEndId).Name("RWY_END_ID");
        Map(m => m.TrueAlignment).Name("TRUE_ALIGNMENT").TypeConverter<OptionalIntConverter>().Optional();
        Map(m => m.ApproachType).Name("ILS_TYPE").Optional();
        Map(m => m.RightHandTrafficPattern).Name("RIGHT_HAND_TRAFFIC_PAT_FLAG").TypeConverter<YesNoToBoolConverter>().Optional();
        Map(m => m.RunwayMarkingsType).Name("RWY_MARKING_TYPE_CODE").Optional();
        Map(m => m.RunwayMarkingsCondition).Name("RWY_MARKING_COND").Optional();
        Map(m => m.LatDecimal).Name("LAT_DECIMAL").TypeConverter<OptionalDecimalConverter>().Optional();
        Map(m => m.LongDecimal).Name("LONG_DECIMAL").TypeConverter<OptionalDecimalConverter>().Optional();
        Map(m => m.Elevation).Name("RWY_END_ELEV").TypeConverter<OptionalDecimalConverter>().Optional();
        Map(m => m.ThresholdCrossingHeight).Name("THR_CROSSING_HGT").TypeConverter<OptionalDecimalConverter>().Optional();
        Map(m => m.VisualGlidePathAngle).Name("VISUAL_GLIDE_PATH_ANGLE").TypeConverter<OptionalDecimalConverter>().Optional();
        Map(m => m.DisplacedThresholdLatDecimal).Name("LAT_DISPLACED_THR_DECIMAL").TypeConverter<OptionalDecimalConverter>().Optional();
        Map(m => m.DisplacedThresholdLongDecimal).Name("LONG_DISPLACED_THR_DECIMAL").TypeConverter<OptionalDecimalConverter>().Optional();
        Map(m => m.DisplacedThresholdElev).Name("DISPLACED_THR_ELEV").TypeConverter<OptionalDecimalConverter>().Optional();
        Map(m => m.DisplacedThresholdLength).Name("DISPLACED_THR_LEN").TypeConverter<OptionalIntConverter>().Optional();
        Map(m => m.TouchdownZoneElev).Name("TDZ_ELEV").TypeConverter<OptionalDecimalConverter>().Optional();
        Map(m => m.VisualGlideSlopeIndicator).Name("VGSI_CODE").Optional();
        Map(m => m.RunwayVisualRangeEquipment).Name("RWY_VISUAL_RANGE_EQUIP_CODE").Optional();
        Map(m => m.RunwayVisibilityValueEquipment).Name("RWY_VSBY_VALUE_EQUIP_FLAG").TypeConverter<YesNoToBoolConverter>().Optional();
        Map(m => m.ApproachLightSystem).Name("APCH_LGT_SYSTEM_CODE").Optional();
        Map(m => m.RunwayEndLights).Name("RWY_END_LGTS_FLAG").TypeConverter<YesNoToBoolConverter>().Optional();
        Map(m => m.CenterlineLights).Name("CNTRLN_LGTS_AVBL_FLAG").TypeConverter<YesNoToBoolConverter>().Optional();
        Map(m => m.TouchdownZoneLights).Name("TDZ_LGT_AVBL_FLAG").TypeConverter<YesNoToBoolConverter>().Optional();
        Map(m => m.ControllingObjectDescription).Name("OBSTN_TYPE").Optional();
        Map(m => m.ControllingObjectMarkedLighted).Name("OBSTN_MRKD_CODE").Optional();
        Map(m => m.ControllingObjectClearanceSlope).Name("OBSTN_CLNC_SLOPE").TypeConverter<OptionalIntConverter>().Optional();
        Map(m => m.ControllingObjectHeightAboveRunway).Name("OBSTN_HGT").TypeConverter<OptionalIntConverter>().Optional();
        Map(m => m.ControllingObjectDistanceFromRunway).Name("DIST_FROM_THR").TypeConverter<OptionalIntConverter>().Optional();
        Map(m => m.ControllingObjectCenterlineOffset).Name("CNTRLN_OFFSET").Optional();

        // DMS Coordinates - Runway End
        Map(m => m.RwyEndLatDeg).Name("RWY_END_LAT_DEG").TypeConverter<OptionalIntConverter>().Optional();
        Map(m => m.RwyEndLatMin).Name("RWY_END_LAT_MIN").TypeConverter<OptionalIntConverter>().Optional();
        Map(m => m.RwyEndLatSec).Name("RWY_END_LAT_SEC").TypeConverter<OptionalDecimalConverter>().Optional();
        Map(m => m.RwyEndLatHemis).Name("RWY_END_LAT_HEMIS").Optional();
        Map(m => m.RwyEndLongDeg).Name("RWY_END_LONG_DEG").TypeConverter<OptionalIntConverter>().Optional();
        Map(m => m.RwyEndLongMin).Name("RWY_END_LONG_MIN").TypeConverter<OptionalIntConverter>().Optional();
        Map(m => m.RwyEndLongSec).Name("RWY_END_LONG_SEC").TypeConverter<OptionalDecimalConverter>().Optional();
        Map(m => m.RwyEndLongHemis).Name("RWY_END_LONG_HEMIS").Optional();

        // DMS Coordinates - Displaced Threshold
        Map(m => m.DisplacedThrLatDeg).Name("DISPLACED_THR_LAT_DEG").TypeConverter<OptionalIntConverter>().Optional();
        Map(m => m.DisplacedThrLatMin).Name("DISPLACED_THR_LAT_MIN").TypeConverter<OptionalIntConverter>().Optional();
        Map(m => m.DisplacedThrLatSec).Name("DISPLACED_THR_LAT_SEC").TypeConverter<OptionalDecimalConverter>().Optional();
        Map(m => m.DisplacedThrLatHemis).Name("DISPLACED_THR_LAT_HEMIS").Optional();
        Map(m => m.DisplacedThrLongDeg).Name("DISPLACED_THR_LONG_DEG").TypeConverter<OptionalIntConverter>().Optional();
        Map(m => m.DisplacedThrLongMin).Name("DISPLACED_THR_LONG_MIN").TypeConverter<OptionalIntConverter>().Optional();
        Map(m => m.DisplacedThrLongSec).Name("DISPLACED_THR_LONG_SEC").TypeConverter<OptionalDecimalConverter>().Optional();
        Map(m => m.DisplacedThrLongHemis).Name("DISPLACED_THR_LONG_HEMIS").Optional();

        // Codes & Gradient
        Map(m => m.FarPart77Code).Name("FAR_PART_77_CODE").Optional();
        Map(m => m.CenterlineDirectionCode).Name("CNTRLN_DIR_CODE").Optional();
        Map(m => m.RunwayGradient).Name("RWY_GRAD").TypeConverter<OptionalDecimalConverter>().Optional();
        Map(m => m.RunwayGradientDirection).Name("RWY_GRAD_DIRECTION").Optional();

        // Source/Date Metadata
        Map(m => m.RwyEndPositionSource).Name("RWY_END_PSN_SOURCE").Optional();
        Map(m => m.RwyEndPositionDate).Name("RWY_END_PSN_DATE").TypeConverter<OptionalDateConverter>().Optional();
        Map(m => m.RwyEndElevationSource).Name("RWY_END_ELEV_SOURCE").Optional();
        Map(m => m.RwyEndElevationDate).Name("RWY_END_ELEV_DATE").TypeConverter<OptionalDateConverter>().Optional();
        Map(m => m.DisplacedThrPositionSource).Name("DSPL_THR_PSN_SOURCE").Optional();
        Map(m => m.DisplacedThrPositionDate).Name("RWY_END_DSPL_THR_PSN_DATE").TypeConverter<OptionalDateConverter>().Optional();
        Map(m => m.DisplacedThrElevationSource).Name("DSPL_THR_ELEV_SOURCE").Optional();
        Map(m => m.DisplacedThrElevationDate).Name("RWY_END_DSPL_THR_ELEV_DATE").TypeConverter<OptionalDateConverter>().Optional();
        Map(m => m.TouchdownZoneElevSource).Name("TDZ_ELEV_SOURCE").Optional();
        Map(m => m.TouchdownZoneElevDate).Name("RWY_END_TDZ_ELEV_DATE").TypeConverter<OptionalDateConverter>().Optional();

        // Declared Distances
        Map(m => m.TakeoffRunAvailable).Name("TKOF_RUN_AVBL").TypeConverter<OptionalIntConverter>().Optional();
        Map(m => m.TakeoffDistanceAvailable).Name("TKOF_DIST_AVBL").TypeConverter<OptionalIntConverter>().Optional();
        Map(m => m.AccelerateStopDistAvailable).Name("ACLT_STOP_DIST_AVBL").TypeConverter<OptionalIntConverter>().Optional();
        Map(m => m.LandingDistanceAvailable).Name("LNDG_DIST_AVBL").TypeConverter<OptionalIntConverter>().Optional();

        // LAHSO
        Map(m => m.LahsoAvailableLandingDistance).Name("LAHSO_ALD").TypeConverter<OptionalIntConverter>().Optional();
        Map(m => m.LahsoIntersectingRunway).Name("RWY_END_INTERSECT_LAHSO").Optional();
        Map(m => m.LahsoDescription).Name("LAHSO_DESC").Optional();
        Map(m => m.LahsoLatitude).Name("LAHSO_LAT").Optional();
        Map(m => m.LahsoLatDecimal).Name("LAT_LAHSO_DECIMAL").TypeConverter<OptionalDecimalConverter>().Optional();
        Map(m => m.LahsoLongitude).Name("LAHSO_LONG").Optional();
        Map(m => m.LahsoLongDecimal).Name("LONG_LAHSO_DECIMAL").TypeConverter<OptionalDecimalConverter>().Optional();
        Map(m => m.LahsoPositionSource).Name("LAHSO_PSN_SOURCE").Optional();
        Map(m => m.LahsoPositionDate).Name("RWY_END_LAHSO_PSN_DATE").TypeConverter<OptionalDateConverter>().Optional();
    }
}

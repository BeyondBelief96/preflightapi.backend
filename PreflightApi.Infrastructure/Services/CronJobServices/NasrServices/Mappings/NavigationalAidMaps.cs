using CsvHelper.Configuration;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Services.CronJobServices.NasrServices.Utils;

namespace PreflightApi.Infrastructure.Services.CronJobServices.NasrServices.Mappings;

public sealed class NavigationalAidMap : ClassMap<NavigationalAid>
{
    public NavigationalAidMap()
    {
        Map(m => m.EffectiveDate).Name("EFF_DATE").TypeConverter<OptionalDateConverter>();
        Map(m => m.NavId).Name("NAV_ID");
        Map(m => m.NavType).Name("NAV_TYPE");
        Map(m => m.StateCode).Name("STATE_CODE").Optional();
        Map(m => m.City).Name("CITY").Optional();
        Map(m => m.CountryCode).Name("COUNTRY_CODE").Optional();
        Map(m => m.NavStatus).Name("NAV_STATUS").Optional();
        Map(m => m.Name).Name("NAME").Optional();
        Map(m => m.StateName).Name("STATE_NAME").Optional();
        Map(m => m.RegionCode).Name("REGION_CODE").Optional();
        Map(m => m.CountryName).Name("COUNTRY_NAME").Optional();
        Map(m => m.FanMarker).Name("FAN_MARKER").Optional();
        Map(m => m.Owner).Name("OWNER").Optional();
        Map(m => m.Operator).Name("OPERATOR").Optional();
        Map(m => m.NasUseFlag).Name("NAS_USE_FLAG").Optional();
        Map(m => m.PublicUseFlag).Name("PUBLIC_USE_FLAG").Optional();
        Map(m => m.NdbClassCode).Name("NDB_CLASS_CODE").Optional();
        Map(m => m.OperHours).Name("OPER_HOURS").Optional();
        Map(m => m.HighAltArtccId).Name("HIGH_ALT_ARTCC_ID").Optional();
        Map(m => m.HighArtccName).Name("HIGH_ARTCC_NAME").Optional();
        Map(m => m.LowAltArtccId).Name("LOW_ALT_ARTCC_ID").Optional();
        Map(m => m.LowArtccName).Name("LOW_ARTCC_NAME").Optional();
        Map(m => m.LatDecimal).Name("LAT_DECIMAL").TypeConverter<OptionalDecimalConverter>();
        Map(m => m.LongDecimal).Name("LONG_DECIMAL").TypeConverter<OptionalDecimalConverter>();
        Map(m => m.SurveyAccuracyCode).Name("SURVEY_ACCURACY_CODE").Optional();
        Map(m => m.TacanDmeStatus).Name("TACAN_DME_STATUS").Optional();
        Map(m => m.TacanDmeLatDecimal).Name("TACAN_DME_LAT_DECIMAL").TypeConverter<OptionalDecimalConverter>();
        Map(m => m.TacanDmeLongDecimal).Name("TACAN_DME_LONG_DECIMAL").TypeConverter<OptionalDecimalConverter>();
        Map(m => m.Elevation).Name("ELEV").TypeConverter<OptionalDecimalConverter>();
        Map(m => m.MagVarn).Name("MAG_VARN").Optional();
        Map(m => m.MagVarnHemis).Name("MAG_VARN_HEMIS").Optional();
        Map(m => m.MagVarnYear).Name("MAG_VARN_YEAR").Optional();
        Map(m => m.SimulVoiceFlag).Name("SIMUL_VOICE_FLAG").Optional();
        Map(m => m.PowerOutput).Name("PWR_OUTPUT").Optional();
        Map(m => m.AutoVoiceIdFlag).Name("AUTO_VOICE_ID_FLAG").Optional();
        Map(m => m.MonitoringCategoryCode).Name("MNT_CAT_CODE").Optional();
        Map(m => m.VoiceCall).Name("VOICE_CALL").Optional();
        Map(m => m.Channel).Name("CHAN").Optional();
        Map(m => m.Frequency).Name("FREQ").Optional();
        Map(m => m.MarkerIdent).Name("MKR_IDENT").Optional();
        Map(m => m.MarkerShape).Name("MKR_SHAPE").Optional();
        Map(m => m.MarkerBearing).Name("MKR_BRG").Optional();
        Map(m => m.AltitudeCode).Name("ALT_CODE").Optional();
        Map(m => m.DmeSsv).Name("DME_SSV").Optional();
        Map(m => m.LowNavOnHighChartFlag).Name("LOW_NAV_ON_HIGH_CHART_FLAG").Optional();
        Map(m => m.ZMarkerFlag).Name("Z_MKR_FLAG").Optional();
        Map(m => m.FssId).Name("FSS_ID").Optional();
        Map(m => m.FssName).Name("FSS_NAME").Optional();
        Map(m => m.FssHours).Name("FSS_HOURS").Optional();
        Map(m => m.NotamId).Name("NOTAM_ID").Optional();
        Map(m => m.QuadIdent).Name("QUAD_IDENT").Optional();
        Map(m => m.PitchFlag).Name("PITCH_FLAG").Optional();
        Map(m => m.CatchFlag).Name("CATCH_FLAG").Optional();
        Map(m => m.SuaAtcaaFlag).Name("SUA_ATCAA_FLAG").Optional();
        Map(m => m.RestrictionFlag).Name("RESTRICTION_FLAG").Optional();
        Map(m => m.HiwasFlag).Name("HIWAS_FLAG").Optional();
    }
}

public record NavCheckpointRecord(
    string NavId,
    string NavType,
    int? Altitude,
    string? Bearing,
    string? AirGroundCode,
    string? Description,
    string? AirportId,
    string? StateCheckCode);

public sealed class NavCheckpointMap : ClassMap<NavCheckpointRecord>
{
    public NavCheckpointMap()
    {
        Map(m => m.NavId).Name("NAV_ID");
        Map(m => m.NavType).Name("NAV_TYPE");
        Map(m => m.Altitude).Name("ALTITUDE").TypeConverter<OptionalIntConverter>();
        Map(m => m.Bearing).Name("BRG").Optional();
        Map(m => m.AirGroundCode).Name("AIR_GND_CODE").Optional();
        Map(m => m.Description).Name("CHK_DESC").Optional();
        Map(m => m.AirportId).Name("ARPT_ID").Optional();
        Map(m => m.StateCheckCode).Name("STATE_CHK_CODE").Optional();
    }
}

public sealed class NavigationalAidRemarkMap : ClassMap<NavigationalAid>
{
    public NavigationalAidRemarkMap()
    {
        Map(m => m.NavId).Name("NAV_ID");
        Map(m => m.NavType).Name("NAV_TYPE");
        Map(m => m.Remarks).Name("REMARK").Optional();
    }
}

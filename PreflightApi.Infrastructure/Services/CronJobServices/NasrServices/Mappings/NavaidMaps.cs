using CsvHelper.Configuration;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.ValueObjects;
using PreflightApi.Infrastructure.Services.CronJobServices.NasrServices.Utils;

namespace PreflightApi.Infrastructure.Services.CronJobServices.NasrServices.Mappings
{
    public sealed class NavaidBaseMap : ClassMap<Navaid>
    {
        public NavaidBaseMap()
        {
            // Common columns
            Map(m => m.EffectiveDate).Name("EFF_DATE").TypeConverter<OptionalDateConverter>();
            Map(m => m.NavId).Name("NAV_ID");
            Map(m => m.NavType).Name("NAV_TYPE");
            Map(m => m.StateCode).Name("STATE_CODE").Optional();
            Map(m => m.City).Name("CITY");
            Map(m => m.CountryCode).Name("COUNTRY_CODE");

            // NAV_BASE specific
            Map(m => m.NavStatus).Name("NAV_STATUS");
            Map(m => m.Name).Name("NAME");
            Map(m => m.StateName).Name("STATE_NAME").Optional();
            Map(m => m.RegionCode).Name("REGION_CODE").Optional();
            Map(m => m.CountryName).Name("COUNTRY_NAME");
            Map(m => m.FanMarker).Name("FAN_MARKER").Optional();
            Map(m => m.Owner).Name("OWNER").Optional();
            Map(m => m.Operator).Name("OPERATOR").Optional();
            Map(m => m.NasUseFlag).Name("NAS_USE_FLAG");
            Map(m => m.PublicUseFlag).Name("PUBLIC_USE_FLAG");
            Map(m => m.NdbClassCode).Name("NDB_CLASS_CODE").Optional();
            Map(m => m.OperHours).Name("OPER_HOURS").Optional();
            Map(m => m.HighAltArtccId).Name("HIGH_ALT_ARTCC_ID").Optional();
            Map(m => m.HighArtccName).Name("HIGH_ARTCC_NAME").Optional();
            Map(m => m.LowAltArtccId).Name("LOW_ALT_ARTCC_ID").Optional();
            Map(m => m.LowArtccName).Name("LOW_ARTCC_NAME").Optional();

            // Coordinates
            Map(m => m.LatDeg).Name("LAT_DEG").TypeConverter<OptionalIntConverter>();
            Map(m => m.LatMin).Name("LAT_MIN").TypeConverter<OptionalIntConverter>();
            Map(m => m.LatSec).Name("LAT_SEC").TypeConverter<OptionalDecimalConverter>();
            Map(m => m.LatHemis).Name("LAT_HEMIS").Optional();
            Map(m => m.LatDecimal).Name("LAT_DECIMAL").TypeConverter<OptionalDecimalConverter>();
            Map(m => m.LongDeg).Name("LONG_DEG").TypeConverter<OptionalIntConverter>();
            Map(m => m.LongMin).Name("LONG_MIN").TypeConverter<OptionalIntConverter>();
            Map(m => m.LongSec).Name("LONG_SEC").TypeConverter<OptionalDecimalConverter>();
            Map(m => m.LongHemis).Name("LONG_HEMIS").Optional();
            Map(m => m.LongDecimal).Name("LONG_DECIMAL").TypeConverter<OptionalDecimalConverter>();
            Map(m => m.SurveyAccuracyCode).Name("SURVEY_ACCURACY_CODE").Optional();

            // TACAN/DME location
            Map(m => m.TacanDmeStatus).Name("TACAN_DME_STATUS").Optional();
            Map(m => m.TacanDmeLatDeg).Name("TACAN_DME_LAT_DEG").TypeConverter<OptionalIntConverter>().Optional();
            Map(m => m.TacanDmeLatMin).Name("TACAN_DME_LAT_MIN").TypeConverter<OptionalIntConverter>().Optional();
            Map(m => m.TacanDmeLatSec).Name("TACAN_DME_LAT_SEC").TypeConverter<OptionalDecimalConverter>().Optional();
            Map(m => m.TacanDmeLatHemis).Name("TACAN_DME_LAT_HEMIS").Optional();
            Map(m => m.TacanDmeLatDecimal).Name("TACAN_DME_LAT_DECIMAL").TypeConverter<OptionalDecimalConverter>().Optional();
            Map(m => m.TacanDmeLongDeg).Name("TACAN_DME_LONG_DEG").TypeConverter<OptionalIntConverter>().Optional();
            Map(m => m.TacanDmeLongMin).Name("TACAN_DME_LONG_MIN").TypeConverter<OptionalIntConverter>().Optional();
            Map(m => m.TacanDmeLongSec).Name("TACAN_DME_LONG_SEC").TypeConverter<OptionalDecimalConverter>().Optional();
            Map(m => m.TacanDmeLongHemis).Name("TACAN_DME_LONG_HEMIS").Optional();
            Map(m => m.TacanDmeLongDecimal).Name("TACAN_DME_LONG_DECIMAL").TypeConverter<OptionalDecimalConverter>().Optional();

            // Other data
            Map(m => m.Elev).Name("ELEV").TypeConverter<OptionalDecimalConverter>().Optional();
            Map(m => m.MagVarn).Name("MAG_VARN").TypeConverter<OptionalIntConverter>().Optional();
            Map(m => m.MagVarnHemis).Name("MAG_VARN_HEMIS").Optional();
            Map(m => m.MagVarnYear).Name("MAG_VARN_YEAR").TypeConverter<OptionalIntConverter>().Optional();
            Map(m => m.SimulVoiceFlag).Name("SIMUL_VOICE_FLAG").Optional();
            Map(m => m.PwrOutput).Name("PWR_OUTPUT").TypeConverter<OptionalIntConverter>().Optional();
            Map(m => m.AutoVoiceIdFlag).Name("AUTO_VOICE_ID_FLAG").Optional();
            Map(m => m.MntCatCode).Name("MNT_CAT_CODE").Optional();
            Map(m => m.VoiceCall).Name("VOICE_CALL").Optional();
            Map(m => m.Chan).Name("CHAN").Optional();
            Map(m => m.Freq).Name("FREQ").TypeConverter<OptionalDecimalConverter>().Optional();
            Map(m => m.MkrIdent).Name("MKR_IDENT").Optional();
            Map(m => m.MkrShape).Name("MKR_SHAPE").Optional();
            Map(m => m.MkrBrg).Name("MKR_BRG").TypeConverter<OptionalIntConverter>().Optional();
            Map(m => m.AltCode).Name("ALT_CODE").Optional();
            Map(m => m.DmeSsv).Name("DME_SSV").Optional();
            Map(m => m.LowNavOnHighChartFlag).Name("LOW_NAV_ON_HIGH_CHART_FLAG").Optional();
            Map(m => m.ZMkrFlag).Name("Z_MKR_FLAG").Optional();
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

    public sealed class NavaidCheckpointMap : ClassMap<NavaidCheckpoint>
    {
        public NavaidCheckpointMap()
        {
            Map(m => m.Altitude).Name("ALTITUDE").TypeConverter<OptionalIntConverter>().Optional();
            Map(m => m.Bearing).Name("BRG").TypeConverter<OptionalIntConverter>();
            Map(m => m.AirGroundCode).Name("AIR_GND_CODE");
            Map(m => m.Description).Name("CHK_DESC");
            Map(m => m.AirportId).Name("ARPT_ID").Optional();
            Map(m => m.StateCode).Name("STATE_CHK_CODE");
        }
    }

    public sealed class NavaidRemarkMap : ClassMap<NavaidRemark>
    {
        public NavaidRemarkMap()
        {
            Map(m => m.TabName).Name("TAB_NAME");
            Map(m => m.ReferenceColumnName).Name("REF_COL_NAME");
            Map(m => m.SequenceNumber).Name("REF_COL_SEQ_NO").TypeConverter<OptionalIntConverter>();
            Map(m => m.Remark).Name("REMARK");
        }
    }
}

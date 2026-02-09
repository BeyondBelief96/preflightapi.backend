using CsvHelper.Configuration;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Services.CronJobServices.NasrServices.Utils;

namespace PreflightApi.Infrastructure.Services.CronJobServices.NasrServices.Mappings;

public record FixChartRecord(
    string FixId,
    string IcaoRegionCode,
    string? StateCode,
    string? CountryCode,
    string? ChartingTypeDesc);

public sealed class FixChartMap : ClassMap<FixChartRecord>
{
    public FixChartMap()
    {
        Map(m => m.FixId).Name("FIX_ID");
        Map(m => m.IcaoRegionCode).Name("ICAO_REGION_CODE");
        Map(m => m.StateCode).Name("STATE_CODE").Optional();
        Map(m => m.CountryCode).Name("COUNTRY_CODE").Optional();
        Map(m => m.ChartingTypeDesc).Name("CHARTING_TYPE_DESC").Optional();
    }
}

public record FixNavRecord(
    string FixId,
    string IcaoRegionCode,
    string? StateCode,
    string? CountryCode,
    string? NavId,
    string? NavType,
    string? Bearing,
    string? Distance);

public sealed class FixNavMap : ClassMap<FixNavRecord>
{
    public FixNavMap()
    {
        Map(m => m.FixId).Name("FIX_ID");
        Map(m => m.IcaoRegionCode).Name("ICAO_REGION_CODE");
        Map(m => m.StateCode).Name("STATE_CODE").Optional();
        Map(m => m.CountryCode).Name("COUNTRY_CODE").Optional();
        Map(m => m.NavId).Name("NAV_ID").Optional();
        Map(m => m.NavType).Name("NAV_TYPE").Optional();
        Map(m => m.Bearing).Name("BEARING").Optional();
        Map(m => m.Distance).Name("DISTANCE").Optional();
    }
}

public sealed class FixMap : ClassMap<Fix>
{
    public FixMap()
    {
        Map(m => m.EffectiveDate).Name("EFF_DATE").TypeConverter<OptionalDateConverter>();
        Map(m => m.FixId).Name("FIX_ID");
        Map(m => m.IcaoRegionCode).Name("ICAO_REGION_CODE");
        Map(m => m.StateCode).Name("STATE_CODE").Optional();
        Map(m => m.CountryCode).Name("COUNTRY_CODE").Optional();
        Map(m => m.LatDecimal).Name("LAT_DECIMAL").TypeConverter<OptionalDecimalConverter>();
        Map(m => m.LongDecimal).Name("LONG_DECIMAL").TypeConverter<OptionalDecimalConverter>();
        Map(m => m.FixIdOld).Name("FIX_ID_OLD").Optional();
        Map(m => m.ChartingRemark).Name("CHARTING_REMARK").Optional();
        Map(m => m.FixUseCode).Name("FIX_USE_CODE").Optional();
        Map(m => m.ArtccIdHigh).Name("ARTCC_ID_HIGH").Optional();
        Map(m => m.ArtccIdLow).Name("ARTCC_ID_LOW").Optional();
        Map(m => m.PitchFlag).Name("PITCH_FLAG").Optional();
        Map(m => m.CatchFlag).Name("CATCH_FLAG").Optional();
        Map(m => m.SuaAtcaaFlag).Name("SUA_ATCAA_FLAG").Optional();
        Map(m => m.MinReceptionAlt).Name("MIN_RECEP_ALT").Optional();
        Map(m => m.Compulsory).Name("COMPULSORY").Optional();
        Map(m => m.Charts).Name("CHARTS").Optional();
    }
}

using CsvHelper.Configuration;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Services.CronJobServices.NasrServices.Utils;

namespace PreflightApi.Infrastructure.Services.CronJobServices.NasrServices.Mappings;

public sealed class WeatherStationMap : ClassMap<WeatherStation>
{
    public WeatherStationMap()
    {
        Map(m => m.EffectiveDate).Name("EFF_DATE").TypeConverter<OptionalDateConverter>();
        Map(m => m.AsosAwosId).Name("ASOS_AWOS_ID");
        Map(m => m.AsosAwosType).Name("ASOS_AWOS_TYPE");
        Map(m => m.StateCode).Name("STATE_CODE").Optional();
        Map(m => m.City).Name("CITY").Optional();
        Map(m => m.CountryCode).Name("COUNTRY_CODE").Optional();
        Map(m => m.CommissionedDate).Name("COMMISSIONED_DATE").TypeConverter<OptionalDateConverter>();
        Map(m => m.NavaidFlag).Name("NAVAID_FLAG").Optional();
        Map(m => m.LatDecimal).Name("LAT_DECIMAL").TypeConverter<OptionalDecimalConverter>();
        Map(m => m.LongDecimal).Name("LONG_DECIMAL").TypeConverter<OptionalDecimalConverter>();
        Map(m => m.Elevation).Name("ELEV").TypeConverter<OptionalDecimalConverter>();
        Map(m => m.SurveyMethodCode).Name("SURVEY_METHOD_CODE").Optional();
        Map(m => m.PhoneNo).Name("PHONE_NO").Optional();
        Map(m => m.SecondPhoneNo).Name("SECOND_PHONE_NO").Optional();
        Map(m => m.SiteNo).Name("SITE_NO").Optional();
        Map(m => m.SiteTypeCode).Name("SITE_TYPE_CODE").Optional();
        Map(m => m.Remarks).Name("REMARK").Optional();
    }
}

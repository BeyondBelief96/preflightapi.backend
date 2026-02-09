using PreflightApi.Domain.Entities;

namespace PreflightApi.Infrastructure.Dtos.Mappers;

public static class WeatherStationMapper
{
    public static WeatherStationDto ToDto(WeatherStation station)
    {
        return new WeatherStationDto
        {
            Id = station.Id,
            AsosAwosId = station.AsosAwosId,
            AsosAwosType = station.AsosAwosType,
            EffectiveDate = station.EffectiveDate,
            StateCode = station.StateCode,
            City = station.City,
            CountryCode = station.CountryCode,
            CommissionedDate = station.CommissionedDate,
            NavaidFlag = station.NavaidFlag,
            LatDecimal = station.LatDecimal,
            LongDecimal = station.LongDecimal,
            Elevation = station.Elevation,
            SurveyMethodCode = station.SurveyMethodCode,
            PhoneNo = station.PhoneNo,
            SecondPhoneNo = station.SecondPhoneNo,
            SiteNo = station.SiteNo,
            SiteTypeCode = station.SiteTypeCode,
            Remarks = station.Remarks
        };
    }
}

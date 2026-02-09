using PreflightApi.Domain.Entities;

namespace PreflightApi.Infrastructure.Dtos.Mappers;

public static class FixMapper
{
    public static FixDto ToDto(Fix fix)
    {
        return new FixDto
        {
            Id = fix.Id,
            FixId = fix.FixId,
            IcaoRegionCode = fix.IcaoRegionCode,
            StateCode = fix.StateCode,
            CountryCode = fix.CountryCode,
            EffectiveDate = fix.EffectiveDate,
            LatDecimal = fix.LatDecimal,
            LongDecimal = fix.LongDecimal,
            FixIdOld = fix.FixIdOld,
            ChartingRemark = fix.ChartingRemark,
            FixUseCode = fix.FixUseCode,
            ArtccIdHigh = fix.ArtccIdHigh,
            ArtccIdLow = fix.ArtccIdLow,
            PitchFlag = fix.PitchFlag,
            CatchFlag = fix.CatchFlag,
            SuaAtcaaFlag = fix.SuaAtcaaFlag,
            MinReceptionAlt = fix.MinReceptionAlt,
            Compulsory = fix.Compulsory,
            Charts = fix.Charts,
            ChartingTypes = fix.ChartingTypes,
            NavaidReferences = fix.NavaidReferences?.Select(n => new FixNavaidReferenceDto
            {
                NavId = n.NavId,
                NavType = n.NavType,
                Bearing = n.Bearing,
                Distance = n.Distance
            }).ToList()
        };
    }
}

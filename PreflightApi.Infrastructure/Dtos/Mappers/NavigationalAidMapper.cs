using PreflightApi.Domain.Entities;

namespace PreflightApi.Infrastructure.Dtos.Mappers;

public static class NavigationalAidMapper
{
    public static NavigationalAidDto ToDto(NavigationalAid navAid)
    {
        return new NavigationalAidDto
        {
            Id = navAid.Id,
            NavId = navAid.NavId,
            NavType = navAid.NavType,
            EffectiveDate = navAid.EffectiveDate,
            StateCode = navAid.StateCode,
            City = navAid.City,
            CountryCode = navAid.CountryCode,
            NavStatus = navAid.NavStatus,
            Name = navAid.Name,
            StateName = navAid.StateName,
            RegionCode = navAid.RegionCode,
            CountryName = navAid.CountryName,
            FanMarker = navAid.FanMarker,
            Owner = navAid.Owner,
            Operator = navAid.Operator,
            NasUseFlag = navAid.NasUseFlag,
            PublicUseFlag = navAid.PublicUseFlag,
            NdbClassCode = navAid.NdbClassCode,
            OperHours = navAid.OperHours,
            HighAltArtccId = navAid.HighAltArtccId,
            HighArtccName = navAid.HighArtccName,
            LowAltArtccId = navAid.LowAltArtccId,
            LowArtccName = navAid.LowArtccName,
            LatDecimal = navAid.LatDecimal,
            LongDecimal = navAid.LongDecimal,
            SurveyAccuracyCode = navAid.SurveyAccuracyCode,
            TacanDmeStatus = navAid.TacanDmeStatus,
            TacanDmeLatDecimal = navAid.TacanDmeLatDecimal,
            TacanDmeLongDecimal = navAid.TacanDmeLongDecimal,
            Elevation = navAid.Elevation,
            MagVarn = navAid.MagVarn,
            MagVarnHemis = navAid.MagVarnHemis,
            MagVarnYear = navAid.MagVarnYear,
            SimulVoiceFlag = navAid.SimulVoiceFlag,
            PowerOutput = navAid.PowerOutput,
            AutoVoiceIdFlag = navAid.AutoVoiceIdFlag,
            MonitoringCategoryCode = navAid.MonitoringCategoryCode,
            VoiceCall = navAid.VoiceCall,
            Channel = navAid.Channel,
            Frequency = navAid.Frequency,
            MarkerIdent = navAid.MarkerIdent,
            MarkerShape = navAid.MarkerShape,
            MarkerBearing = navAid.MarkerBearing,
            AltitudeCode = navAid.AltitudeCode,
            DmeSsv = navAid.DmeSsv,
            LowNavOnHighChartFlag = navAid.LowNavOnHighChartFlag,
            ZMarkerFlag = navAid.ZMarkerFlag,
            FssId = navAid.FssId,
            FssName = navAid.FssName,
            FssHours = navAid.FssHours,
            NotamId = navAid.NotamId,
            QuadIdent = navAid.QuadIdent,
            PitchFlag = navAid.PitchFlag,
            CatchFlag = navAid.CatchFlag,
            SuaAtcaaFlag = navAid.SuaAtcaaFlag,
            RestrictionFlag = navAid.RestrictionFlag,
            HiwasFlag = navAid.HiwasFlag,
            Remarks = navAid.Remarks,
            Checkpoints = navAid.Checkpoints?.Select(c => new NavaidCheckpointDto
            {
                Altitude = c.Altitude,
                Bearing = c.Bearing,
                AirGroundCode = c.AirGroundCode,
                Description = c.Description,
                AirportId = c.AirportId,
                StateCheckCode = c.StateCheckCode
            }).ToList()
        };
    }
}

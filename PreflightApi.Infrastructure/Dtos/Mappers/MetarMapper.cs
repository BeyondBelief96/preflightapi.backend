using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.Dtos.Mappers;

public static class MetarMapper
{
    private static readonly Dictionary<string, FlightCategory> FlightCategoryMap = new()
    {
        ["VFR"] = FlightCategory.VFR,
        ["MVFR"] = FlightCategory.MVFR,
        ["IFR"] = FlightCategory.IFR,
        ["LIFR"] = FlightCategory.LIFR
    };

    private static readonly Dictionary<string, SkyCover> SkyCoverMap = new()
    {
        ["SKC"] = SkyCover.SKC,
        ["CLR"] = SkyCover.CLR,
        ["FEW"] = SkyCover.FEW,
        ["SCT"] = SkyCover.SCT,
        ["BKN"] = SkyCover.BKN,
        ["OVC"] = SkyCover.OVC,
        ["OVX"] = SkyCover.OVX
    };

    public static MetarDto ToDto(Metar metar, ILogger logger)
    {
        return new MetarDto
        {
            Id = metar.Id,
            RawText = metar.RawText,
            StationId = metar.StationId,
            ObservationTime = metar.ObservationTime,
            Latitude = metar.Latitude,
            Longitude = metar.Longitude,
            TempC = metar.TempC,
            DewpointC = metar.DewpointC,
            WindDirDegrees = metar.WindDirDegrees,
            WindSpeedKt = metar.WindSpeedKt,
            WindGustKt = metar.WindGustKt,
            VisibilityStatuteMi = metar.VisibilityStatuteMi,
            AltimInHg = metar.AltimInHg,
            SeaLevelPressureMb = metar.SeaLevelPressureMb,
            QualityControlFlags = metar.QualityControlFlags != null
                ? new MetarQualityControlFlagsDto
                {
                    Corrected = ParseQcFlag(metar.QualityControlFlags.Corrected),
                    Auto = ParseQcFlag(metar.QualityControlFlags.Auto),
                    AutoStation = ParseQcFlag(metar.QualityControlFlags.AutoStation),
                    MaintenanceIndicatorOn = ParseQcFlag(metar.QualityControlFlags.MaintenanceIndicatorOn),
                    NoSignal = ParseQcFlag(metar.QualityControlFlags.NoSignal),
                    LightningSensorOff = ParseQcFlag(metar.QualityControlFlags.LightningSensorOff),
                    FreezingRainSensorOff = ParseQcFlag(metar.QualityControlFlags.FreezingRainSensorOff),
                    PresentWeatherSensorOff = ParseQcFlag(metar.QualityControlFlags.PresentWeatherSensorOff)
                }
                : null,
            WxString = metar.WxString,
            SkyCondition = metar.SkyCondition?.Select(sc => new MetarSkyConditionDto
            {
                SkyCover = EnumParseHelper.Parse(sc.SkyCover, logger, nameof(sc.SkyCover), nameof(Metar), metar.StationId ?? metar.Id.ToString(), SkyCoverMap),
                CloudBaseFtAgl = sc.CloudBaseFtAgl
            }).ToList(),
            FlightCategory = EnumParseHelper.Parse(metar.FlightCategory, logger, nameof(metar.FlightCategory), nameof(Metar), metar.StationId ?? metar.Id.ToString(), FlightCategoryMap)
        };
    }

    private static bool? ParseQcFlag(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim().Equals("TRUE", StringComparison.OrdinalIgnoreCase);
    }
}

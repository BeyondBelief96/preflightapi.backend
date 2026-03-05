using System.Text.Json;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Enums;
using PreflightApi.Domain.ValueObjects;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.Dtos.Mappers;

public static class NavaidMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Dictionary<string, NavaidType> NavaidTypeMap = new()
    {
        ["CONSOLAN"] = NavaidType.Consolan,
        ["DME"] = NavaidType.Dme,
        ["FAN MARKER"] = NavaidType.FanMarker,
        ["MARINE NDB"] = NavaidType.MarineNdb,
        ["MARINE NDB/DME"] = NavaidType.MarineNdbDme,
        ["NDB"] = NavaidType.Ndb,
        ["NDB/DME"] = NavaidType.NdbDme,
        ["TACAN"] = NavaidType.Tacan,
        ["UHF/NDB"] = NavaidType.UhfNdb,
        ["VOR"] = NavaidType.Vor,
        ["VORTAC"] = NavaidType.Vortac,
        ["VOR/DME"] = NavaidType.VorDme,
        ["VOT"] = NavaidType.Vot
    };

    private static readonly Dictionary<string, VorServiceVolume> VorServiceVolumeMap = new()
    {
        ["H"] = VorServiceVolume.High,
        ["L"] = VorServiceVolume.Low,
        ["T"] = VorServiceVolume.Terminal,
        ["VH"] = VorServiceVolume.VorHigh,
        ["VL"] = VorServiceVolume.VorLow
    };

    private static readonly Dictionary<string, DmeServiceVolume> DmeServiceVolumeMap = new()
    {
        ["H"] = DmeServiceVolume.High,
        ["L"] = DmeServiceVolume.Low,
        ["T"] = DmeServiceVolume.Terminal,
        ["DH"] = DmeServiceVolume.DmeHigh,
        ["DL"] = DmeServiceVolume.DmeLow
    };

    public static NavaidDto ToDto(Navaid navaid, ILogger logger)
    {
        var id = navaid.NavId ?? navaid.Id.ToString();

        return new NavaidDto
        {
            Id = navaid.Id,
            NavId = navaid.NavId,
            NavType = EnumParseHelper.Parse(navaid.NavType, logger, nameof(navaid.NavType), nameof(Navaid), id, NavaidTypeMap),
            NavStatus = navaid.NavStatus,
            Name = navaid.Name,
            City = navaid.City,
            StateCode = navaid.StateCode,
            StateName = navaid.StateName,
            CountryCode = navaid.CountryCode,
            CountryName = navaid.CountryName,
            Owner = navaid.Owner,
            Operator = navaid.Operator,
            NasUse = ParseBool(navaid.NasUseFlag),
            PublicUse = ParseBool(navaid.PublicUseFlag),
            Latitude = navaid.LatDecimal,
            Longitude = navaid.LongDecimal,
            Elevation = navaid.Elev,
            MagneticVariation = navaid.MagVarn,
            MagneticVariationDirection = navaid.MagVarnHemis,
            MagneticVariationYear = navaid.MagVarnYear,
            Frequency = navaid.Freq,
            Channel = navaid.Chan,
            VoiceCall = navaid.VoiceCall,
            OperatingHours = navaid.OperHours,
            NdbClassCode = navaid.NdbClassCode,
            AltCode = EnumParseHelper.Parse(navaid.AltCode, logger, nameof(navaid.AltCode), nameof(Navaid), id, VorServiceVolumeMap),
            DmeSsv = EnumParseHelper.Parse(navaid.DmeSsv, logger, nameof(navaid.DmeSsv), nameof(Navaid), id, DmeServiceVolumeMap),
            SimultaneousVoice = ParseBool(navaid.SimulVoiceFlag),
            AutomaticVoiceId = ParseBool(navaid.AutoVoiceIdFlag),
            Hiwas = ParseBool(navaid.HiwasFlag),
            LowNavOnHighChart = ParseBool(navaid.LowNavOnHighChartFlag),
            PowerOutput = navaid.PwrOutput,
            TacanDmeStatus = navaid.TacanDmeStatus,
            TacanDmeLatitude = navaid.TacanDmeLatDecimal,
            TacanDmeLongitude = navaid.TacanDmeLongDecimal,
            HighAltArtccId = navaid.HighAltArtccId,
            HighArtccName = navaid.HighArtccName,
            LowAltArtccId = navaid.LowAltArtccId,
            LowArtccName = navaid.LowArtccName,
            FssId = navaid.FssId,
            FssName = navaid.FssName,
            NotamId = navaid.NotamId,
            EffectiveDate = navaid.EffectiveDate,
            Checkpoints = DeserializeJson<List<NavaidCheckpoint>>(navaid.CheckpointsJson),
            Remarks = DeserializeJson<List<NavaidRemark>>(navaid.RemarksJson)
        };
    }

    /// <summary>
    /// Converts a <see cref="NavaidType"/> enum value back to the FAA database string
    /// used for filtering queries.
    /// </summary>
    public static string ToDbString(NavaidType type)
    {
        return type switch
        {
            NavaidType.Consolan => "CONSOLAN",
            NavaidType.Dme => "DME",
            NavaidType.FanMarker => "FAN MARKER",
            NavaidType.MarineNdb => "MARINE NDB",
            NavaidType.MarineNdbDme => "MARINE NDB/DME",
            NavaidType.Ndb => "NDB",
            NavaidType.NdbDme => "NDB/DME",
            NavaidType.Tacan => "TACAN",
            NavaidType.UhfNdb => "UHF/NDB",
            NavaidType.Vor => "VOR",
            NavaidType.Vortac => "VORTAC",
            NavaidType.VorDme => "VOR/DME",
            NavaidType.Vot => "VOT",
            _ => type.ToString()
        };
    }

    internal static bool ParseBool(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Trim().ToUpperInvariant() == "Y";
    }

    internal static T? DeserializeJson<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

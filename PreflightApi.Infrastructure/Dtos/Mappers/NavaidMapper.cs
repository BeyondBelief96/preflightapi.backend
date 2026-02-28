using System.Text.Json;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Enums;
using PreflightApi.Domain.ValueObjects;

namespace PreflightApi.Infrastructure.Dtos.Mappers;

public static class NavaidMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static NavaidDto ToDto(Navaid navaid)
    {
        return new NavaidDto
        {
            Id = navaid.Id,
            NavId = navaid.NavId,
            NavType = ParseNavaidType(navaid.NavType),
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
            AltCode = ParseVorServiceVolume(navaid.AltCode),
            DmeSsv = ParseDmeServiceVolume(navaid.DmeSsv),
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

    internal static NavaidType ParseNavaidType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return NavaidType.Unknown;

        return value.Trim().ToUpperInvariant() switch
        {
            "CONSOLAN" => NavaidType.Consolan,
            "DME" => NavaidType.Dme,
            "FAN MARKER" => NavaidType.FanMarker,
            "MARINE NDB" => NavaidType.MarineNdb,
            "MARINE NDB/DME" => NavaidType.MarineNdbDme,
            "NDB" => NavaidType.Ndb,
            "NDB/DME" => NavaidType.NdbDme,
            "TACAN" => NavaidType.Tacan,
            "UHF/NDB" => NavaidType.UhfNdb,
            "VOR" => NavaidType.Vor,
            "VORTAC" => NavaidType.Vortac,
            "VOR/DME" => NavaidType.VorDme,
            "VOT" => NavaidType.Vot,
            _ => NavaidType.Unknown
        };
    }

    internal static VorServiceVolume? ParseVorServiceVolume(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim().ToUpperInvariant() switch
        {
            "H" => VorServiceVolume.High,
            "L" => VorServiceVolume.Low,
            "T" => VorServiceVolume.Terminal,
            "VH" => VorServiceVolume.VorHigh,
            "VL" => VorServiceVolume.VorLow,
            _ => VorServiceVolume.Unknown
        };
    }

    internal static DmeServiceVolume? ParseDmeServiceVolume(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim().ToUpperInvariant() switch
        {
            "H" => DmeServiceVolume.High,
            "L" => DmeServiceVolume.Low,
            "T" => DmeServiceVolume.Terminal,
            "DH" => DmeServiceVolume.DmeHigh,
            "DL" => DmeServiceVolume.DmeLow,
            _ => DmeServiceVolume.Unknown
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

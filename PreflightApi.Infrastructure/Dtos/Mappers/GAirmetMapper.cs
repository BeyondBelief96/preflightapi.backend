using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.Dtos.Mappers;

public static class GAirmetMapper
{
    private static readonly Dictionary<string, GAirmetProduct> ProductMap = new()
    {
        ["SIERRA"] = GAirmetProduct.SIERRA,
        ["TANGO"] = GAirmetProduct.TANGO,
        ["ZULU"] = GAirmetProduct.ZULU
    };

    public static GAirmetDto ToDto(GAirmet gairmet, ILogger logger)
    {
        return new GAirmetDto
        {
            Id = gairmet.Id,
            ReceiptTime = gairmet.ReceiptTime,
            IssueTime = gairmet.IssueTime,
            ExpireTime = gairmet.ExpireTime,
            ValidTime = gairmet.ValidTime,
            Product = EnumParseHelper.Parse(gairmet.Product, logger, nameof(gairmet.Product), nameof(GAirmet), gairmet.Id.ToString(), ProductMap),
            Tag = gairmet.Tag,
            ForecastHour = gairmet.ForecastHour,
            Hazard = ParseHazardType(gairmet.HazardType),
            HazardSeverity = ParseSeverity(gairmet.HazardSeverity),
            GeometryType = gairmet.GeometryType,
            DueTo = gairmet.DueTo,
            Altitudes = gairmet.Altitudes,
            Area = gairmet.Area
        };
    }

    private static HazardSeverity? ParseSeverity(string? severity)
    {
        if (string.IsNullOrEmpty(severity)) return null;

        return severity.Trim().ToUpperInvariant() switch
        {
            "LGT" => HazardSeverity.LGT,
            "LT-MOD" or "LT_MOD" => HazardSeverity.LT_MOD,
            "MOD" => HazardSeverity.MOD,
            "MOD-SEV" or "MOD_SEV" => HazardSeverity.MOD_SEV,
            "SEV" => HazardSeverity.SEV,
            _ => null
        };
    }

    public static GAirmetHazardType? ParseHazardType(string? hazardType)
    {
        if (string.IsNullOrEmpty(hazardType)) return null;

        return hazardType.ToUpperInvariant() switch
        {
            // SIERRA hazards
            "MT_OBSC" => GAirmetHazardType.MT_OBSC,
            "IFR" => GAirmetHazardType.IFR,

            // TANGO hazards
            "TURB-LO" => GAirmetHazardType.TURB_LO,
            "TURB-HI" => GAirmetHazardType.TURB_HI,
            "LLWS" => GAirmetHazardType.LLWS,
            "SFC_WIND" => GAirmetHazardType.SFC_WIND,
            "SFC-WIND" => GAirmetHazardType.SFC_WIND,

            // ZULU hazards
            "ICE" => GAirmetHazardType.ICE,
            "FZLVL" => GAirmetHazardType.FZLVL,
            "M_FZLVL" => GAirmetHazardType.M_FZLVL,

            _ => null
        };
    }

    public static string? ConvertHazardTypeToString(GAirmetHazardType hazardType)
    {
        return hazardType switch
        {
            GAirmetHazardType.MT_OBSC => "MT_OBSC",
            GAirmetHazardType.IFR => "IFR",
            GAirmetHazardType.TURB_LO => "TURB-LO",
            GAirmetHazardType.TURB_HI => "TURB-HI",
            GAirmetHazardType.LLWS => "LLWS",
            GAirmetHazardType.SFC_WIND => "SFC_WIND",
            GAirmetHazardType.ICE => "ICE",
            GAirmetHazardType.FZLVL => "FZLVL",
            GAirmetHazardType.M_FZLVL => "M_FZLVL",
            _ => null
        };
    }
}

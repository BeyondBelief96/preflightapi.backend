using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Enums;
using PreflightApi.Domain.ValueObjects.Sigmets;

namespace PreflightApi.Infrastructure.Dtos.Mappers;

public static class SigmetMapper
{
    public static SigmetDto ToDto(Sigmet sigmet)
    {
        return new SigmetDto
        {
            Id = sigmet.Id,
            RawText = sigmet.RawText,
            ValidTimeFrom = sigmet.ValidTimeFrom,
            ValidTimeTo = sigmet.ValidTimeTo,
            Altitude = sigmet.Altitude,
            MovementDirDegrees = sigmet.MovementDirDegrees,
            MovementSpeedKt = sigmet.MovementSpeedKt,
            Hazard = MapHazard(sigmet.Hazard),
            SigmetType = sigmet.SigmetType,
            Areas = sigmet.Areas
        };
    }

    private static SigmetHazardDto? MapHazard(SigmetHazard? hazard)
    {
        if (hazard == null) return null;

        return new SigmetHazardDto
        {
            Type = ParseHazardType(hazard.Type),
            Severity = hazard.Severity
        };
    }

    private static SigmetHazardType? ParseHazardType(string? type)
    {
        if (string.IsNullOrEmpty(type)) return null;

        return type.ToUpperInvariant() switch
        {
            "CONVECTIVE" => SigmetHazardType.CONVECTIVE,
            "ICE" => SigmetHazardType.ICE,
            "TURB" => SigmetHazardType.TURB,
            "IFR" => SigmetHazardType.IFR,
            "MTN OBSCN" => SigmetHazardType.MTN_OBSCN,
            _ => null
        };
    }
}

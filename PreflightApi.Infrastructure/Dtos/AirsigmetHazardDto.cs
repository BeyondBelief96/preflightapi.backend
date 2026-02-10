using PreflightApi.Domain.Enums;

namespace PreflightApi.Infrastructure.Dtos;

/// <summary>
/// Hazard type and severity information for an AIRMET or SIGMET advisory.
/// </summary>
public record AirsigmetHazardDto
{
    /// <summary>The weather hazard type: CONVECTIVE (thunderstorms/convection), ICE (icing), TURB (turbulence), IFR (instrument flight rules conditions), or MTN_OBSCN (mountain obscuration).</summary>
    public AirsigmetHazardType? Type { get; init; }

    /// <summary>Hazard severity. AIRMETs use LGT (light) or MOD (moderate). SIGMETs use MOD-SEV (moderate to severe) or SEV (severe). Convective SIGMETs do not have a severity value.</summary>
    public string? Severity { get; init; }
}

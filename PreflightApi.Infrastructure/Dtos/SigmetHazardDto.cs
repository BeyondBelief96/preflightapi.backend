using PreflightApi.Domain.Enums;

namespace PreflightApi.Infrastructure.Dtos;

/// <summary>
/// Hazard type and severity information for a SIGMET advisory.
/// </summary>
public record SigmetHazardDto
{
    /// <summary>The weather hazard type: CONVECTIVE (thunderstorms/convection), ICE (icing), TURB (turbulence), IFR (instrument flight rules conditions), ASH (volcanic ash), or MTN_OBSCN (mountain obscuration).</summary>
    public SigmetHazardType? Type { get; init; }

    /// <summary>Hazard severity: LGT (light), LT_MOD (light to moderate), MOD (moderate, typical for AIRMET), MOD_SEV (moderate to severe), SEV (severe, typical for SIGMET). Convective SIGMETs do not have a severity value.</summary>
    public HazardSeverity? Severity { get; init; }
}

namespace PreflightApi.Domain.ValueObjects.Sigmets
{
    /// <summary>
    /// Hazard type and severity for an AIRMET/SIGMET advisory.
    /// </summary>
    public class SigmetHazard
    {
        /// <summary>
        /// The hazard type: TURB (turbulence), ICE (icing), IFR (instrument flight rules), CONVECTIVE (thunderstorms/convection), ASH (volcanic ash), or MTN OBSCN (mountain obscuration).
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// The hazard severity: LGT (light), LT-MOD (light to moderate), MOD (moderate, typical for AIRMET), MOD-SEV (moderate to severe), SEV (severe, typical for SIGMET). Convective SIGMETs do not have a severity value.
        /// </summary>
        public string? Severity { get; set; }
    }
}

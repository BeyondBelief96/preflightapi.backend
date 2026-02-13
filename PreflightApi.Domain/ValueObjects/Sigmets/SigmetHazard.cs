namespace PreflightApi.Domain.ValueObjects.Sigmets
{
    public class SigmetHazard
    {
        /// <summary>
        /// The hazard: TURB, ICE, IFR, CONVECTIVE, ASH, MTN OBSCN
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// The severity: MOD-SEV, SEV (SIGMET)
        /// </summary>
        public string? Severity { get; set; }
    }
}

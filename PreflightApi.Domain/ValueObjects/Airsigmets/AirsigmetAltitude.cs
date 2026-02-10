namespace PreflightApi.Domain.ValueObjects.Airsigmets
{
    /// <summary>
    /// Altitude range for an AIRMET or SIGMET advisory.
    /// </summary>
    public class AirsigmetAltitude
    {
        /// <summary>Minimum altitude in feet MSL for the advisory area.</summary>
        public int? MinFtMsl { get; set; }
        /// <summary>Maximum altitude in feet MSL for the advisory area.</summary>
        public int? MaxFtMsl { get; set; }
    }
}

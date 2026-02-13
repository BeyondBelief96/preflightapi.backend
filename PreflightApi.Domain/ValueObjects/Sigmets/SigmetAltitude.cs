namespace PreflightApi.Domain.ValueObjects.Sigmets
{
    /// <summary>
    /// Altitude range for a SIGMET advisory.
    /// </summary>
    public class SigmetAltitude
    {
        /// <summary>Minimum altitude in feet MSL for the advisory area.</summary>
        public int? MinFtMsl { get; set; }
        /// <summary>Maximum altitude in feet MSL for the advisory area.</summary>
        public int? MaxFtMsl { get; set; }
    }
}

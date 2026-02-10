namespace PreflightApi.Domain.ValueObjects.Pireps
{
    /// <summary>
    /// A sky condition layer reported by a pilot in a PIREP.
    /// </summary>
    public class PirepSkyCondition
    {
        /// <summary>Sky cover type: SKC (sky clear), CLR (clear), FEW (few), SCT (scattered), BKN (broken), OVC (overcast), or OVX (obscured).</summary>
        public string SkyCover { get; set; } = string.Empty;
        /// <summary>Cloud base altitude in feet MSL (note: PIREP altitudes are MSL, unlike TAF/METAR which use AGL).</summary>
        public int? CloudBaseFtMsl { get; set; }
        /// <summary>Cloud top altitude in feet MSL.</summary>
        public int? CloudTopFtMsl { get; set; }
    }
}

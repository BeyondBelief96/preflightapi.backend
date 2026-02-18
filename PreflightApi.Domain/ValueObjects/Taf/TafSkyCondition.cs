namespace PreflightApi.Domain.ValueObjects.Taf
{
    /// <summary>
    /// A single sky condition layer in a TAF forecast period.
    /// </summary>
    public class TafSkyCondition
    {
        /// <summary>Sky cover type: CLR (clear), CAVOK (ceiling and visibility OK), FEW (few), SCT (scattered), BKN (broken), OVC (overcast), or OVX (obscured).</summary>
        public string SkyCover { get; set; } = string.Empty;
        /// <summary>Cloud base height in feet AGL. Null for CLR or CAVOK.</summary>
        public int? CloudBaseFtAgl { get; set; }
        /// <summary>Cloud type modifier (e.g., CB for cumulonimbus, TCU for towering cumulus).</summary>
        public string? CloudType { get; set; }
    }
}

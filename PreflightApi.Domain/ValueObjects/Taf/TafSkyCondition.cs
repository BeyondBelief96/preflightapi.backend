namespace PreflightApi.Domain.ValueObjects.Taf
{
    /// <summary>
    /// A single sky condition layer in a TAF forecast period.
    /// </summary>
    public class TafSkyCondition
    {
        /// <summary>Sky cover type: SKC (sky clear), CLR (clear), FEW (few), SCT (scattered), BKN (broken), or OVC (overcast).</summary>
        public string SkyCover { get; set; } = string.Empty;
        /// <summary>Cloud base height in feet AGL. Null for SKC or CLR.</summary>
        public int? CloudBaseFtAgl { get; set; }
        /// <summary>Cloud type modifier (e.g., CB for cumulonimbus, TCU for towering cumulus).</summary>
        public string? CloudType { get; set; }
    }
}

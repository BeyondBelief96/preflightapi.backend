namespace PreflightApi.Domain.ValueObjects.Metar
{
    /// <summary>
    /// A single sky condition layer in a METAR observation. Maximum 4 layers per observation per the XSD schema.
    /// </summary>
    public class MetarSkyCondition
    {
        /// <summary>Sky cover type: SKC (sky clear), CLR (clear below 12,000), FEW (few), SCT (scattered), BKN (broken), OVC (overcast), or OVX (obscured).</summary>
        public string SkyCover { get; set; } = string.Empty;
        /// <summary>Cloud base height in feet AGL. Null for SKC or CLR.</summary>
        public int? CloudBaseFtAgl { get; set; }
    }
}

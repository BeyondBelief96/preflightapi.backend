namespace PreflightApi.Domain.ValueObjects.Pireps
{
    /// <summary>
    /// An icing condition reported by a pilot in a PIREP.
    /// </summary>
    public class PirepIcingCondition
    {
        /// <summary>Icing type: RIME (rime ice), CLEAR (clear ice), or MIXED (mixed rime and clear).</summary>
        public string? IcingType { get; set; }
        /// <summary>Icing intensity: NEG (none), NEGclr (none, clear of clouds), TRC (trace), TRC-LGT (trace to light), LGT (light), LGT-MOD (light to moderate), MOD (moderate), MOD-SEV (moderate to severe), HVY (heavy/severe), or SEV (severe).</summary>
        public string? IcingIntensity { get; set; }
        /// <summary>Bottom of the icing layer in feet MSL.</summary>
        public int? IcingBaseFtMsl { get; set; }
        /// <summary>Top of the icing layer in feet MSL.</summary>
        public int? IcingTopFtMsl { get; set; }
    }
}

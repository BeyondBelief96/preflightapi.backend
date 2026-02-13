namespace PreflightApi.Domain.ValueObjects.Pireps
{
    /// <summary>
    /// A turbulence condition reported by a pilot in a PIREP.
    /// </summary>
    public class PirepTurbulenceCondition
    {
        /// <summary>Turbulence type: CAT (clear air), CHOP (chop), LLWS (low-level wind shear), or MWAVE (mountain wave).</summary>
        public string? TurbulenceType { get; set; }
        /// <summary>Turbulence intensity: NEG (none), SMTH-LGT (smooth to light), LGT (light), LGT-MOD (light to moderate), MOD (moderate), MOD-SEV (moderate to severe), SEV (severe), SEV-EXTM (severe to extreme), or EXTM (extreme).</summary>
        public string? TurbulenceIntensity { get; set; }
        /// <summary>Bottom of the turbulence layer in feet MSL.</summary>
        public int? TurbulenceBaseFtMsl { get; set; }
        /// <summary>Top of the turbulence layer in feet MSL.</summary>
        public int? TurbulenceTopFtMsl { get; set; }
        /// <summary>Turbulence frequency: ISOL (isolated), OCNL (occasional), or CONT (continuous).</summary>
        public string? TurbulenceFreq { get; set; }
    }
}

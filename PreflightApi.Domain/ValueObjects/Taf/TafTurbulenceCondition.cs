namespace PreflightApi.Domain.ValueObjects.Taf
{
    /// <summary>
    /// Forecast turbulence condition within a TAF forecast period.
    /// </summary>
    public class TafTurbulenceCondition
    {
        /// <summary>Turbulence intensity code (integer 0-9 per TAF implementation table): 0 (none), 1 (light), 2 (moderate occasional), 3 (moderate frequent), 4 (severe), 5 (extreme), 6-8 (reserved), 9 (not specified), or X (mountain wave).</summary>
        public string? TurbulenceIntensity { get; set; }
        /// <summary>Bottom of the turbulence layer in feet AGL.</summary>
        public int? TurbulenceMinAltFtAgl { get; set; }
        /// <summary>Top of the turbulence layer in feet AGL.</summary>
        public int? TurbulenceMaxAltFtAgl { get; set; }
    }
}

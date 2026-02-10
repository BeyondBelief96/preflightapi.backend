namespace PreflightApi.Domain.ValueObjects.Taf
{
    /// <summary>
    /// Forecast icing condition within a TAF forecast period.
    /// </summary>
    public class TafIcingCondition
    {
        /// <summary>Icing intensity code: 0 (none), 1 (light), 2 (light in clouds), 3 (light in precipitation), 4 (moderate), 5 (moderate in clouds), 6 (moderate in precipitation), 7 (severe), 8 (severe in clouds), 9 (severe in precipitation).</summary>
        public string? IcingIntensity { get; set; }
        /// <summary>Bottom of the icing layer in feet AGL.</summary>
        public int? IcingMinAltFtAgl { get; set; }
        /// <summary>Top of the icing layer in feet AGL.</summary>
        public int? IcingMaxAltFtAgl { get; set; }
    }
}

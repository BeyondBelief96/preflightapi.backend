namespace PreflightApi.Domain.ValueObjects.Metar
{
    /// <summary>
    /// Quality control flags indicating METAR observation characteristics and sensor status.
    /// </summary>
    public class MetarQualityControlFlags
    {
        /// <summary>Indicates a corrected observation (COR in raw METAR).</summary>
        public string? Corrected { get; set; }
        /// <summary>Indicates a fully automated observation (AUTO in raw METAR).</summary>
        public string? Auto { get; set; }
        /// <summary>Indicates the observation came from an automated station type (AO1 or AO2).</summary>
        public string? AutoStation { get; set; }
        /// <summary>Maintenance indicator is on, signaling a possible sensor malfunction.</summary>
        public string? MaintenanceIndicatorOn { get; set; }
        /// <summary>No signal received from the station.</summary>
        public string? NoSignal { get; set; }
        /// <summary>Lightning sensor is off or not operating.</summary>
        public string? LightningSensorOff { get; set; }
        /// <summary>Freezing rain sensor is off or not operating.</summary>
        public string? FreezingRainSensorOff { get; set; }
        /// <summary>Present weather sensor is off or not operating.</summary>
        public string? PresentWeatherSensorOff { get; set; }
    }
}

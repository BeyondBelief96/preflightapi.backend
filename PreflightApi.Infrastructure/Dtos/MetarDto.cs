namespace PreflightApi.Infrastructure.Dtos
{
    /// <summary>
    /// METAR (Meteorological Aerodrome Report) observation data for an airport.
    /// </summary>
    public record MetarDto
    {
        /// <summary>Database identifier.</summary>
        public int Id { get; init; }
        /// <summary>Raw METAR text string as received from the source.</summary>
        public string? RawText { get; init; }
        /// <summary>ICAO station identifier (e.g., KDFW).</summary>
        public string? StationId { get; init; }
        /// <summary>Observation time in ISO 8601 format.</summary>
        public string? ObservationTime { get; init; }
        /// <summary>Station latitude in decimal degrees.</summary>
        public float? Latitude { get; init; }
        /// <summary>Station longitude in decimal degrees.</summary>
        public float? Longitude { get; init; }
        /// <summary>Temperature in degrees Celsius.</summary>
        public float? TempC { get; init; }
        /// <summary>Dewpoint temperature in degrees Celsius.</summary>
        public float? DewpointC { get; init; }
        /// <summary>Wind direction in degrees true, or "VRB" for variable.</summary>
        public string? WindDirDegrees { get; init; }
        /// <summary>Wind speed in knots.</summary>
        public int? WindSpeedKt { get; init; }
        /// <summary>Wind gust speed in knots.</summary>
        public int? WindGustKt { get; init; }
        /// <summary>Visibility in statute miles.</summary>
        public string? VisibilityStatuteMi { get; init; }
        /// <summary>Altimeter setting in inches of mercury.</summary>
        public float? AltimInHg { get; init; }
        /// <summary>Sea level pressure in millibars.</summary>
        public float? SeaLevelPressureMb { get; init; }
        /// <summary>Quality control flags for the observation.</summary>
        public MetarQualityControlFlagsDto? QualityControlFlags { get; init; }
        /// <summary>Present weather string (e.g., "-RA" for light rain).</summary>
        public string? WxString { get; init; }
        /// <summary>Sky condition layers (cloud cover and bases).</summary>
        public List<MetarSkyConditionDto>? SkyCondition { get; init; } = [];
        /// <summary>Flight category: VFR, MVFR, IFR, or LIFR.</summary>
        public string? FlightCategory { get; init; }
    }

    /// <summary>
    /// Quality control flags indicating METAR observation characteristics.
    /// </summary>
    public record MetarQualityControlFlagsDto
    {
        /// <summary>Indicates a corrected observation.</summary>
        public string? Corrected { get; init; }
        /// <summary>Indicates an automated observation.</summary>
        public string? Auto { get; init; }
        /// <summary>Indicates an automated station type.</summary>
        public string? AutoStation { get; init; }
        /// <summary>Maintenance indicator is on.</summary>
        public string? MaintenanceIndicatorOn { get; init; }
        /// <summary>No signal received.</summary>
        public string? NoSignal { get; init; }
        /// <summary>Lightning sensor is off.</summary>
        public string? LightningSensorOff { get; init; }
        /// <summary>Freezing rain sensor is off.</summary>
        public string? FreezingRainSensorOff { get; init; }
        /// <summary>Present weather sensor is off.</summary>
        public string? PresentWeatherSensorOff { get; init; }
    }

    /// <summary>
    /// A single sky condition layer in a METAR observation.
    /// </summary>
    public record MetarSkyConditionDto
    {
        /// <summary>Sky cover type: SKC, CLR, FEW, SCT, BKN, or OVC.</summary>
        public string SkyCover { get; init; } = string.Empty;
        /// <summary>Cloud base height in feet AGL.</summary>
        public int? CloudBaseFtAgl { get; init; }
    }
}

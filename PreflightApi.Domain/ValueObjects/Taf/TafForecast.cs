namespace PreflightApi.Domain.ValueObjects.Taf
{
    /// <summary>
    /// A single forecast period within a TAF. Each TAF contains one or more forecast periods
    /// covering different time ranges, with optional change indicators (TEMPO, BECMG, FM, PROB).
    /// </summary>
    public class TafForecast
    {
        /// <summary>Start of this forecast period in ISO 8601 format (UTC).</summary>
        public string? FcstTimeFrom { get; set; }
        /// <summary>End of this forecast period in ISO 8601 format (UTC).</summary>
        public string? FcstTimeTo { get; set; }
        /// <summary>Change indicator: FM (from), BECMG (becoming), TEMPO (temporary), or PROB (probability). Null for the base forecast.</summary>
        public string? ChangeIndicator { get; set; }
        /// <summary>Time at which a BECMG (becoming) change completes, in ISO 8601 format (UTC).</summary>
        public string? TimeBecoming { get; set; }
        /// <summary>Probability percentage (e.g., 30 or 40) for PROB-type forecast periods.</summary>
        public int? Probability { get; set; }
        /// <summary>Forecast wind direction in degrees true, or "VRB" for variable winds.</summary>
        public string? WindDirDegrees { get; set; }
        /// <summary>Forecast wind speed in knots.</summary>
        public int? WindSpeedKt { get; set; }
        /// <summary>Forecast wind gust speed in knots.</summary>
        public int? WindGustKt { get; set; }
        /// <summary>Low-level wind shear height in feet AGL.</summary>
        public short? WindShearHgtFtAgl { get; set; }
        /// <summary>Low-level wind shear direction in degrees true.</summary>
        public short? WindShearDirDegrees { get; set; }
        /// <summary>Low-level wind shear speed in knots.</summary>
        public int? WindShearSpeedKt { get; set; }
        /// <summary>Forecast visibility in statute miles. May contain "6+" for visibility greater than 6 miles.</summary>
        public string? VisibilityStatuteMi { get; set; }
        /// <summary>Forecast altimeter setting in inches of mercury.</summary>
        public double? AltimInHg { get; set; }
        /// <summary>Vertical visibility in feet, reported when the sky is obscured.</summary>
        public short? VertVisFt { get; set; }
        /// <summary>Forecast weather phenomena string (e.g., "-RA" for light rain, "+TSRA" for heavy thunderstorms with rain).</summary>
        public string? WxString { get; set; }
        /// <summary>Portion of the TAF text that could not be decoded by the parser.</summary>
        public string? NotDecoded { get; set; }
        /// <summary>Forecast sky condition layers (cloud cover and bases) for this period.</summary>
        public List<TafSkyCondition>? SkyConditions { get; set; }
        /// <summary>Forecast turbulence conditions for this period.</summary>
        public List<TafTurbulenceCondition>? TurbulenceConditions { get; set; }
        /// <summary>Forecast icing conditions for this period.</summary>
        public List<TafIcingCondition>? IcingConditions { get; set; }
        /// <summary>Forecast temperature data for this period.</summary>
        public List<TafTemperature>? Temperature { get; set; }
    }
}

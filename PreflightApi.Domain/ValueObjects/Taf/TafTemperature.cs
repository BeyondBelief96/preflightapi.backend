namespace PreflightApi.Domain.ValueObjects.Taf
{
    /// <summary>
    /// Forecast temperature data within a TAF forecast period.
    /// </summary>
    public class TafTemperature
    {
        /// <summary>Valid time for this temperature forecast in ISO 8601 format (UTC).</summary>
        public string? ValidTime { get; set; }
        /// <summary>Forecast surface temperature in degrees Celsius.</summary>
        public double? SfcTempC { get; set; }
        /// <summary>Forecast maximum temperature in degrees Celsius.</summary>
        public double? MaxTempC { get; set; }
        /// <summary>Forecast minimum temperature in degrees Celsius.</summary>
        public double? MinTempC { get; set; }
    }
}

using PreflightApi.Domain.Enums;
using PreflightApi.Domain.ValueObjects.Pireps;

namespace PreflightApi.Infrastructure.Dtos
{
    /// <summary>
    /// PIREP (Pilot Report) data including turbulence, icing, and sky conditions.
    /// </summary>
    public record PirepDto
    {
        /// <summary>Database identifier.</summary>
        public int Id { get; init; }
        /// <summary>Raw PIREP text string.</summary>
        public string? RawText { get; init; }
        /// <summary>Time the PIREP was received.</summary>
        public string? ReceiptTime { get; init; }
        /// <summary>Time of the pilot observation.</summary>
        public string? ObservationTime { get; init; }
        /// <summary>Quality control flags for the report.</summary>
        public PirepQualityControlFlags? QualityControlFlags { get; init; }
        /// <summary>Aircraft type designation. ex: B738, C172</summary>
        public string? AircraftRef { get; init; }
        /// <summary>Latitude of the report in decimal degrees.</summary>
        public double? Latitude { get; init; }
        /// <summary>Longitude of the report in decimal degrees.</summary>
        public double? Longitude { get; init; }
        /// <summary>Altitude of the report in feet MSL.</summary>
        public int? AltitudeFtMsl { get; init; }
        /// <summary>Reported sky conditions.</summary>
        public List<PirepSkyCondition>? SkyConditions { get; init; } = [];
        /// <summary>Reported turbulence conditions.</summary>
        public List<PirepTurbulenceCondition>? TurbulenceConditions { get; init; } = [];
        /// <summary>Reported icing conditions.</summary>
        public List<PirepIcingCondition>? IcingConditions { get; init; } = [];
        /// <summary>Flight visibility in statute miles.</summary>
        public int? VisibilityStatuteMi { get; init; }
        /// <summary>Present weather string.</summary>
        public string? WxString { get; init; }
        /// <summary>Temperature in degrees Celsius.</summary>
        public double? TempC { get; init; }
        /// <summary>Wind direction in degrees true.</summary>
        public int? WindDirDegrees { get; init; }
        /// <summary>Wind speed in knots.</summary>
        public int? WindSpeedKt { get; init; }
        /// <summary>Vertical gust speed in knots.</summary>
        public int? VertGustKt { get; init; }
        /// <summary>Report type: UA (routine PIREP) or UUA (urgent PIREP).</summary>
        public PirepReportType? ReportType { get; init; }
    }
}

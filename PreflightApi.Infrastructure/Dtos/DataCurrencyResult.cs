namespace PreflightApi.Infrastructure.Dtos
{
    public class DataCurrencyResult
    {
        /// <summary>Identifier of the data synchronization type (e.g., "Metar", "Airport").</summary>
        public required string SyncType { get; init; }
        /// <summary>Whether the data is considered fresh (within staleness thresholds).</summary>
        public bool IsFresh { get; init; }
        /// <summary>Severity level: "ok", "warning", or "critical".</summary>
        public required string Severity { get; init; }
        /// <summary>Staleness evaluation mode: "time-based" or "cycle-based".</summary>
        public required string StalenessMode { get; init; }
        /// <summary>Timestamp of the last successful sync. ISO 8601 UTC format.</summary>
        public DateTime? LastSuccessfulSync { get; init; }
        /// <summary>Number of consecutive sync failures since the last success.</summary>
        public int ConsecutiveFailures { get; init; }
        /// <summary>Error message from the most recent sync failure, if any.</summary>
        public string? LastErrorMessage { get; init; }
        /// <summary>Age of the data since the last successful sync, in minutes. Only present for time-based staleness.</summary>
        public double? AgeMinutes { get; init; }
        /// <summary>Maximum acceptable age before data is considered stale, in minutes. Only present for time-based staleness.</summary>
        public int? ThresholdMinutes { get; init; }
        /// <summary>The date of the current expected data cycle (e.g., NASR 28-day cycle). ISO 8601 UTC format. Only present for cycle-based staleness.</summary>
        public DateTime? CurrentCycleDate { get; init; }
        /// <summary>Number of days past the cycle date without a successful update. Only present for cycle-based staleness.</summary>
        public double? DaysPastCycleWithoutUpdate { get; init; }
        /// <summary>Human-readable status message describing the data currency state.</summary>
        public required string Message { get; init; }
        /// <summary>Timestamp of the last alert sent for this sync type. ISO 8601 UTC format.</summary>
        public DateTime? LastAlertSentUtc { get; init; }
        /// <summary>Severity of the last alert sent (e.g., "warning", "critical").</summary>
        public string? LastAlertSeverity { get; init; }
    }
}

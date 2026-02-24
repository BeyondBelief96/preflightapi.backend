namespace PreflightApi.Infrastructure.Dtos
{
    public class DataFreshnessResult
    {
        public required string SyncType { get; init; }
        public bool IsFresh { get; init; }
        public required string Severity { get; init; }
        public required string StalenessMode { get; init; }
        public DateTime? LastSuccessfulSync { get; init; }
        public int ConsecutiveFailures { get; init; }
        public string? LastErrorMessage { get; init; }
        public double? AgeMinutes { get; init; }
        public int? ThresholdMinutes { get; init; }
        public DateTime? CurrentCycleDate { get; init; }
        public double? DaysPastCycleWithoutUpdate { get; init; }
        public required string Message { get; init; }
        public DateTime? LastAlertSentUtc { get; init; }
        public string? LastAlertSeverity { get; init; }
    }
}

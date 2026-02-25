namespace PreflightApi.Infrastructure.Dtos
{
    public class DataFreshnessSummary
    {
        public int Total { get; init; }
        public int Fresh { get; init; }
        public int Stale { get; init; }
        public Dictionary<string, int> BySeverity { get; init; } = new();
    }

    public class DataFreshnessResponse
    {
        public DateTime CheckedAt { get; init; }
        public required string OverallStatus { get; init; }
        public required DataFreshnessSummary Summary { get; init; }
        public required IReadOnlyList<DataFreshnessResult> DataTypes { get; init; }
    }
}

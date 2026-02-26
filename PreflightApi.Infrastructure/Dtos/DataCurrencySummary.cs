namespace PreflightApi.Infrastructure.Dtos
{
    public class DataCurrencySummary
    {
        public int Total { get; init; }
        public int Fresh { get; init; }
        public int Stale { get; init; }
        public Dictionary<string, int> BySeverity { get; init; } = new();
    }

    public class DataCurrencyResponse
    {
        public DateTime CheckedAt { get; init; }
        public required string OverallStatus { get; init; }
        public required DataCurrencySummary Summary { get; init; }
        public required IReadOnlyList<DataCurrencyResult> DataTypes { get; init; }
    }
}

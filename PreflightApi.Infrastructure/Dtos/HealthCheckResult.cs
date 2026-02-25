namespace PreflightApi.Infrastructure.Dtos
{
    public class HealthCheckEntry
    {
        public required string Name { get; init; }
        public required string Status { get; init; }
        public double Duration { get; init; }
        public string? Description { get; init; }
        public IEnumerable<string> Tags { get; init; } = [];
        public string? Exception { get; init; }
    }

    public class HealthCheckResponse
    {
        public required string Status { get; init; }
        public required string Version { get; init; }
        public double TotalDuration { get; init; }
        public IEnumerable<HealthCheckEntry> Checks { get; init; } = [];
    }
}

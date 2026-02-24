namespace PreflightApi.Infrastructure.Dtos
{
    public class DataWarning
    {
        public required string SyncType { get; init; }
        public required string Severity { get; init; }
        public required string Message { get; init; }
        public DateTime? LastSuccessfulSync { get; init; }
    }
}

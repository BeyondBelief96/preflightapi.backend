namespace PreflightApi.Infrastructure.Interfaces;

public interface ISyncTelemetryService
{
    void TrackSyncCompleted(string syncType, int recordCount, int errorCount, long durationMs);
    void TrackSyncFailed(string syncType, Exception exception, long durationMs);
}

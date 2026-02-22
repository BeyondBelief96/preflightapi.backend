using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Infrastructure.Services.Telemetry;

public class SyncTelemetryService : ISyncTelemetryService
{
    private readonly TelemetryClient? _telemetryClient;
    private readonly ILogger<SyncTelemetryService> _logger;

    public SyncTelemetryService(ILogger<SyncTelemetryService> logger, TelemetryClient? telemetryClient = null)
    {
        _logger = logger;
        _telemetryClient = telemetryClient;
    }

    public void TrackSyncCompleted(string syncType, int recordCount, int errorCount, long durationMs)
    {
        if (_telemetryClient is null)
        {
            _logger.LogDebug(
                "SyncCompleted: {SyncType} — {RecordCount} records, {ErrorCount} errors, {DurationMs}ms",
                syncType, recordCount, errorCount, durationMs);
            return;
        }

        var evt = new EventTelemetry("SyncCompleted");
        evt.Properties["SyncType"] = syncType;
        evt.Properties["RecordCount"] = recordCount.ToString();
        evt.Properties["ErrorCount"] = errorCount.ToString();
        evt.Properties["DurationMs"] = durationMs.ToString();
        _telemetryClient.TrackEvent(evt);

        if (errorCount > 0)
        {
            var metric = new MetricTelemetry("SyncErrorCount", errorCount);
            metric.Properties["SyncType"] = syncType;
            _telemetryClient.TrackMetric(metric);
        }
    }

    public void TrackSyncFailed(string syncType, Exception exception, long durationMs)
    {
        if (_telemetryClient is null)
        {
            _logger.LogDebug(
                "SyncFailed: {SyncType} — {ExceptionType}: {Message}, {DurationMs}ms",
                syncType, exception.GetType().Name, exception.Message, durationMs);
            return;
        }

        var evt = new EventTelemetry("SyncFailed");
        evt.Properties["SyncType"] = syncType;
        evt.Properties["ExceptionType"] = exception.GetType().Name;
        evt.Properties["ErrorMessage"] = exception.Message;
        evt.Properties["DurationMs"] = durationMs.ToString();
        _telemetryClient.TrackEvent(evt);
    }
}

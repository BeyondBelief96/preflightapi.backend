using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Settings;

namespace PreflightApi.Azure.Functions.Functions;

public class DataFreshnessAlertFunction
{
    private readonly IDataSyncStatusService _syncStatusService;
    private readonly IEmailNotificationService _emailService;
    private readonly ResendSettings _settings;
    private readonly ILogger _logger;

    private static readonly Dictionary<string, int> SeverityRanks = new()
    {
        ["none"] = 0,
        ["info"] = 1,
        ["warning"] = 2,
        ["critical"] = 3
    };

    public DataFreshnessAlertFunction(
        IDataSyncStatusService syncStatusService,
        IEmailNotificationService emailService,
        IOptions<ResendSettings> settings,
        ILoggerFactory loggerFactory)
    {
        _syncStatusService = syncStatusService;
        _emailService = emailService;
        _settings = settings.Value;
        _logger = loggerFactory.CreateLogger<DataFreshnessAlertFunction>();
    }

    [Function("DataFreshnessAlertFunction")]
    [ExponentialBackoffRetry(3, "00:00:30", "00:05:00")]
    public async Task Run(
        [TimerTrigger("0 */5 * * * *", RunOnStartup = false)] TimerInfo myTimer,
        FunctionContext context)
    {
        _logger.LogInformation("DataFreshnessAlertFunction executed at: {Time}", DateTime.UtcNow);

        var ct = context.CancellationToken;
        var allFreshness = await _syncStatusService.GetAllFreshnessAsync(ct);
        var now = DateTime.UtcNow;
        var quietPeriod = TimeSpan.FromMinutes(_settings.QuietPeriodMinutes);

        // Identify types needing alerts (severity >= warning)
        // Skip types that have never synced successfully — they haven't had a chance yet
        // (e.g. fresh deployment before cron jobs run). Only alert once data was flowing and then stopped.
        var typesNeedingAlert = new List<DataFreshnessResult>();
        foreach (var item in allFreshness)
        {
            if (!SeverityRanks.TryGetValue(item.Severity, out var rank) || rank < SeverityRanks["warning"])
                continue;

            if (item.LastSuccessfulSync == null)
            {
                _logger.LogDebug("Skipping alert for {SyncType} — has never synced successfully", item.SyncType);
                continue;
            }

            var shouldAlert =
                item.LastAlertSentUtc == null
                || SeverityEscalated(item.LastAlertSeverity, item.Severity)
                || (now - item.LastAlertSentUtc.Value) > quietPeriod;

            if (shouldAlert)
                typesNeedingAlert.Add(item);
        }

        // Identify recovered types: had a prior alert but now fresh
        var recoveredTypes = allFreshness
            .Where(item => item.LastAlertSeverity != null && item.IsFresh)
            .Select(item => item.SyncType)
            .ToList();

        // Send staleness alerts
        if (typesNeedingAlert.Count > 0)
        {
            _logger.LogInformation("Sending staleness alert for {Count} type(s): {Types}",
                typesNeedingAlert.Count,
                string.Join(", ", typesNeedingAlert.Select(t => t.SyncType)));

            await _emailService.SendStalenessAlertAsync(typesNeedingAlert, ct);

            foreach (var item in typesNeedingAlert)
            {
                await _syncStatusService.UpdateAlertStateAsync(item.SyncType, item.Severity, ct);
            }
        }

        // Send recovery notices
        if (recoveredTypes.Count > 0)
        {
            _logger.LogInformation("Sending recovery notice for {Count} type(s): {Types}",
                recoveredTypes.Count,
                string.Join(", ", recoveredTypes));

            await _emailService.SendRecoveryNoticeAsync(recoveredTypes, ct);

            foreach (var type in recoveredTypes)
            {
                await _syncStatusService.ClearAlertStateAsync(type, ct);
            }
        }

        if (typesNeedingAlert.Count == 0 && recoveredTypes.Count == 0)
        {
            _logger.LogInformation("No alerting or recovery actions needed");
        }
    }

    private static bool SeverityEscalated(string? previousSeverity, string currentSeverity)
    {
        if (previousSeverity == null) return true;

        SeverityRanks.TryGetValue(previousSeverity, out var prevRank);
        SeverityRanks.TryGetValue(currentSeverity, out var currRank);

        return currRank > prevRank;
    }
}

using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Constants;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Azure.Functions.Functions;

public class NotamDeltaSyncFunction
{
    private readonly ILogger _logger;
    private readonly INotamDeltaSyncCronService _notamDeltaSyncService;
    private readonly IDataSyncStatusService _syncStatusService;

    public NotamDeltaSyncFunction(
        INotamDeltaSyncCronService notamDeltaSyncService,
        IDataSyncStatusService syncStatusService,
        ILoggerFactory loggerFactory)
    {
        _notamDeltaSyncService = notamDeltaSyncService ?? throw new ArgumentNullException(nameof(notamDeltaSyncService));
        _syncStatusService = syncStatusService ?? throw new ArgumentNullException(nameof(syncStatusService));
        _logger = loggerFactory.CreateLogger<NotamDeltaSyncFunction>();
    }

    [Function("NotamDeltaSyncFunction")]
    [ExponentialBackoffRetry(3, "00:00:30", "00:05:00")]
    public async Task Run([TimerTrigger("0 */3 * * * *", RunOnStartup = FunctionDefaults.RunOnStartup)] TimerInfo myTimer, FunctionContext context)
    {
        _logger.LogInformation("NOTAM Delta Sync Function executed at: {Time}", DateTime.UtcNow);
        var sw = Stopwatch.StartNew();
        try
        {
            await _notamDeltaSyncService.SyncDeltaAsync(context.CancellationToken);
            await _syncStatusService.RecordSuccessAsync(SyncTypes.NotamDelta, ct: context.CancellationToken);
            _logger.LogInformation("NOTAM Delta Sync Function completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            try { await _syncStatusService.RecordFailureAsync(SyncTypes.NotamDelta, ex.Message, context.CancellationToken); }
            catch (Exception inner) { _logger.LogWarning(inner, "Failed to record sync failure for NOTAM Delta"); }
            throw;
        }
    }
}

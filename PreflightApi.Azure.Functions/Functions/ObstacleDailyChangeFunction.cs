using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Constants;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Azure.Functions.Functions;

public class ObstacleDailyChangeFunction
{
    private readonly ILogger _logger;
    private readonly IObstacleDailyChangeCronService _dailyChangeService;
    private readonly IDataSyncStatusService _syncStatusService;

    public ObstacleDailyChangeFunction(
        IObstacleDailyChangeCronService dailyChangeService,
        IDataSyncStatusService syncStatusService,
        ILoggerFactory loggerFactory)
    {
        _dailyChangeService = dailyChangeService ?? throw new ArgumentNullException(nameof(dailyChangeService));
        _syncStatusService = syncStatusService ?? throw new ArgumentNullException(nameof(syncStatusService));
        _logger = loggerFactory.CreateLogger<ObstacleDailyChangeFunction>();
    }

    [Function("ObstacleDailyChangeFunction")]
    [ExponentialBackoffRetry(5, "00:00:30", "00:15:00")]
    public async Task Run([TimerTrigger("0 30 10 * * *", RunOnStartup = false)] TimerInfo myTimer, FunctionContext context)
    {
        _logger.LogInformation("Obstacle Daily Change Function executed at: {Time}", DateTime.UtcNow);
        var sw = Stopwatch.StartNew();
        try
        {
            await _dailyChangeService.ProcessDailyChangesAsync(context.CancellationToken);
            await _syncStatusService.RecordSuccessAsync(SyncTypes.ObstacleDailyChange, ct: context.CancellationToken);
            _logger.LogInformation("Obstacle Daily Change Function completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            try { await _syncStatusService.RecordFailureAsync(SyncTypes.ObstacleDailyChange, ex.Message, context.CancellationToken); }
            catch (Exception inner) { _logger.LogWarning(inner, "Failed to record sync failure for ObstacleDailyChange"); }
            throw;
        }
    }
}

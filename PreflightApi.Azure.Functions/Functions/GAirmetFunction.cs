using System;
using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Constants;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Azure.Functions.Functions;

public class GAirmetFunction
{
    private readonly ILogger _logger;
    private readonly IAviationWeatherService<GAirmet> _gairmetService;
    private readonly IDataSyncStatusService _syncStatusService;

    public GAirmetFunction(
        IAviationWeatherService<GAirmet> gairmetService,
        IDataSyncStatusService syncStatusService,
        ILoggerFactory loggerFactory)
    {
        _gairmetService = gairmetService ?? throw new ArgumentNullException(nameof(gairmetService));
        _syncStatusService = syncStatusService ?? throw new ArgumentNullException(nameof(syncStatusService));
        _logger = loggerFactory.CreateLogger<GAirmetFunction>();
    }

    [Function("GAirmetFunction")]
    [ExponentialBackoffRetry(3, "00:00:30", "00:05:00")]
    public async Task Run([TimerTrigger("0 */30 * * * *", RunOnStartup = FunctionDefaults.RunOnStartup)] TimerInfo myTimer, FunctionContext context)
    {
        _logger.LogInformation("G-AIRMET Function executed at: {Time}", DateTime.UtcNow);
        var sw = Stopwatch.StartNew();
        try
        {
            await _gairmetService.PollWeatherDataAsync(context.CancellationToken);
            await _syncStatusService.RecordSuccessAsync(SyncTypes.GAirmet, ct: context.CancellationToken);
            _logger.LogInformation("G-AIRMET Function completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            try { await _syncStatusService.RecordFailureAsync(SyncTypes.GAirmet, ex.Message, context.CancellationToken); }
            catch (Exception inner) { _logger.LogWarning(inner, "Failed to record sync failure for G-AIRMET"); }
            throw;
        }
    }
}

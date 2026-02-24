using System;
using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Constants;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Azure.Functions.Functions;

public class TafFunction
{
    private readonly ILogger _logger;
    private readonly IAviationWeatherService<Taf> _tafService;
    private readonly IDataSyncStatusService _syncStatusService;

    public TafFunction(
        IAviationWeatherService<Taf> tafService,
        IDataSyncStatusService syncStatusService,
        ILoggerFactory loggerFactory)
    {
        _tafService = tafService ?? throw new ArgumentNullException(nameof(tafService));
        _syncStatusService = syncStatusService ?? throw new ArgumentNullException(nameof(syncStatusService));
        _logger = loggerFactory.CreateLogger<TafFunction>();
    }

    [Function("TafFunction")]
    [ExponentialBackoffRetry(3, "00:00:30", "00:05:00")]
    public async Task Run([TimerTrigger("0 */30 * * * *", RunOnStartup = false)] TimerInfo myTimer, FunctionContext context)
    {
        _logger.LogInformation("TAF Function executed at: {Time}", DateTime.UtcNow);
        var sw = Stopwatch.StartNew();
        try
        {
            await _tafService.PollWeatherDataAsync(context.CancellationToken);
            await _syncStatusService.RecordSuccessAsync(SyncTypes.Taf, ct: context.CancellationToken);
            _logger.LogInformation("TAF Function completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            try { await _syncStatusService.RecordFailureAsync(SyncTypes.Taf, ex.Message, context.CancellationToken); }
            catch (Exception inner) { _logger.LogWarning(inner, "Failed to record sync failure for TAF"); }
            throw;
        }
    }
}

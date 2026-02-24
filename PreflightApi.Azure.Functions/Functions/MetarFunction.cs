using System;
using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Constants;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Azure.Functions.Functions;

public class MetarFunction
{
    private readonly ILogger _logger;
    private readonly IAviationWeatherService<Metar> _metarService;
    private readonly IDataSyncStatusService _syncStatusService;

    public MetarFunction(
        IAviationWeatherService<Metar> metarService,
        IDataSyncStatusService syncStatusService,
        ILoggerFactory loggerFactory)
    {
        _metarService = metarService ?? throw new ArgumentNullException(nameof(metarService));
        _syncStatusService = syncStatusService ?? throw new ArgumentNullException(nameof(syncStatusService));
        _logger = loggerFactory.CreateLogger<MetarFunction>();
    }

    [Function("MetarFunction")]
    [ExponentialBackoffRetry(3, "00:00:30", "00:05:00")]
    public async Task Run([TimerTrigger("0 */10 * * * *", RunOnStartup = false)] TimerInfo myTimer, FunctionContext context)
    {
        _logger.LogInformation("METAR Function executed at: {Time}", DateTime.UtcNow);
        var sw = Stopwatch.StartNew();
        try
        {
            await _metarService.PollWeatherDataAsync(context.CancellationToken);
            await _syncStatusService.RecordSuccessAsync(SyncTypes.Metar, ct: context.CancellationToken);
            _logger.LogInformation("METAR Function completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            try { await _syncStatusService.RecordFailureAsync(SyncTypes.Metar, ex.Message, context.CancellationToken); }
            catch (Exception inner) { _logger.LogWarning(inner, "Failed to record sync failure for METAR"); }
            throw;
        }
    }
}

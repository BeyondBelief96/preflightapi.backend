using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Azure.Functions.Functions;

public class NotamDeltaSyncFunction
{
    private readonly ILogger _logger;
    private readonly INotamDeltaSyncCronService _notamDeltaSyncService;

    public NotamDeltaSyncFunction(INotamDeltaSyncCronService notamDeltaSyncService, ILoggerFactory loggerFactory)
    {
        _notamDeltaSyncService = notamDeltaSyncService ?? throw new ArgumentNullException(nameof(notamDeltaSyncService));
        _logger = loggerFactory.CreateLogger<NotamDeltaSyncFunction>();
    }

    [Function("NotamDeltaSyncFunction")]
    [ExponentialBackoffRetry(3, "00:00:30", "00:05:00")]
    public async Task Run([TimerTrigger("0 */3 * * * *", RunOnStartup = false)] TimerInfo myTimer, FunctionContext context)
    {
        _logger.LogInformation("NOTAM Delta Sync Function executed at: {Time}", DateTime.UtcNow);
        var sw = Stopwatch.StartNew();
        await _notamDeltaSyncService.SyncDeltaAsync(context.CancellationToken);
        _logger.LogInformation("NOTAM Delta Sync Function completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
    }
}

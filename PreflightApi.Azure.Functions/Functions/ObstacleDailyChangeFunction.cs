using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Azure.Functions.Functions;

public class ObstacleDailyChangeFunction
{
    private readonly ILogger _logger;
    private readonly IObstacleDailyChangeCronService _dailyChangeService;

    public ObstacleDailyChangeFunction(
        IObstacleDailyChangeCronService dailyChangeService,
        ILoggerFactory loggerFactory)
    {
        _dailyChangeService = dailyChangeService ?? throw new ArgumentNullException(nameof(dailyChangeService));
        _logger = loggerFactory.CreateLogger<ObstacleDailyChangeFunction>();
    }

    [Function("ObstacleDailyChangeFunction")]
    [ExponentialBackoffRetry(5, "00:00:30", "00:15:00")]
    public async Task Run([TimerTrigger("0 30 10 * * *", RunOnStartup = false)] TimerInfo myTimer, FunctionContext context)
    {
        _logger.LogInformation("Obstacle Daily Change Function executed at: {Time}", DateTime.UtcNow);
        var sw = Stopwatch.StartNew();
        await _dailyChangeService.ProcessDailyChangesAsync(context.CancellationToken);
        _logger.LogInformation("Obstacle Daily Change Function completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
    }
}

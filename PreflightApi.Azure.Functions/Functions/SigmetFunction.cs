using System;
using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Azure.Functions.Functions;

public class SigmetFunction
{
    private readonly ILogger _logger;
    private readonly IAviationWeatherService<Sigmet> _sigmetService;

    public SigmetFunction(IAviationWeatherService<Sigmet> sigmetService, ILoggerFactory loggerFactory)
    {
        _sigmetService = sigmetService ?? throw new ArgumentNullException(nameof(sigmetService));
        _logger = loggerFactory.CreateLogger<SigmetFunction>();
    }

    [Function("SigmetFunction")]
    [ExponentialBackoffRetry(3, "00:00:30", "00:05:00")]
    public async Task Run([TimerTrigger("0 */30 * * * *", RunOnStartup = false)] TimerInfo myTimer, FunctionContext context)
    {
        _logger.LogInformation("SIGMET Function executed at: {Time}", DateTime.UtcNow);
        var sw = Stopwatch.StartNew();
        await _sigmetService.PollWeatherDataAsync(context.CancellationToken);
        _logger.LogInformation("SIGMET Function completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
    }
}

using System;
using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Azure.Functions.Functions;

public class GAirmetFunction
{
    private readonly ILogger _logger;
    private readonly IAviationWeatherService<GAirmet> _gairmetService;

    public GAirmetFunction(IAviationWeatherService<GAirmet> gairmetService, ILoggerFactory loggerFactory)
    {
        _gairmetService = gairmetService ?? throw new ArgumentNullException(nameof(gairmetService));
        _logger = loggerFactory.CreateLogger<GAirmetFunction>();
    }

    [Function("GAirmetFunction")]
    public async Task Run([TimerTrigger("0 */30 * * * *", RunOnStartup = true)] TimerInfo myTimer, FunctionContext context)
    {
        _logger.LogInformation("G-AIRMET Function executed at: {Time}", DateTime.UtcNow);
        var sw = Stopwatch.StartNew();
        await _gairmetService.PollWeatherDataAsync(context.CancellationToken);
        _logger.LogInformation("G-AIRMET Function completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
    }
}

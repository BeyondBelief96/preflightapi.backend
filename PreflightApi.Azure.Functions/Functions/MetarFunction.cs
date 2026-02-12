using System;
using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Azure.Functions.Functions;

public class MetarFunction
{
    private readonly ILogger _logger;
    private readonly IAviationWeatherService<Metar> _metarService;

    public MetarFunction(IAviationWeatherService<Metar> metarService, ILoggerFactory loggerFactory)
    {
        _metarService = metarService ?? throw new ArgumentNullException(nameof(metarService));
        _logger = loggerFactory.CreateLogger<MetarFunction>();
    }

    [Function("MetarFunction")]
    public async Task Run([TimerTrigger("0 */10 * * * *", RunOnStartup = true)] TimerInfo myTimer, FunctionContext context)
    {
        _logger.LogInformation("METAR Function executed at: {Time}", DateTime.UtcNow);
        var sw = Stopwatch.StartNew();
        await _metarService.PollWeatherDataAsync(context.CancellationToken);
        _logger.LogInformation("METAR Function completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
    }
}
using System;
using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Azure.Functions.Functions;

public class PirepFunction
{
    private readonly ILogger _logger;
    private readonly IAviationWeatherService<Pirep> _pirepService;

    public PirepFunction(IAviationWeatherService<Pirep> pirepService, ILoggerFactory loggerFactory)
    {
        _pirepService = pirepService ?? throw new ArgumentNullException(nameof(pirepService));
        _logger = loggerFactory.CreateLogger<PirepFunction>();
    }

    [Function("PirepFunction")]
    [ExponentialBackoffRetry(3, "00:00:30", "00:05:00")]
    public async Task Run([TimerTrigger("0 */5 * * * *", RunOnStartup = false)] TimerInfo myTimer, FunctionContext context)
    {
        _logger.LogInformation("PIREP Function executed at: {Time}", DateTime.UtcNow);
        var sw = Stopwatch.StartNew();
        await _pirepService.PollWeatherDataAsync(context.CancellationToken);
        _logger.LogInformation("PIREP Function completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
    }
}
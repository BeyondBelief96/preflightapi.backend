using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Azure.Functions.Functions;

public class AirsigmetFunction
{
    private readonly ILogger _logger;
    private readonly IAviationWeatherService<Airsigmet> _airsigmetService;

    public AirsigmetFunction(IAviationWeatherService<Airsigmet> airsigmetService, ILoggerFactory loggerFactory)
    {
        _airsigmetService = airsigmetService ?? throw new ArgumentNullException(nameof(airsigmetService));
        _logger = loggerFactory.CreateLogger<AirsigmetFunction>();
    }

    [Function("AirsigmetFunction")]
    public async Task Run([TimerTrigger("0 */30 * * * *", RunOnStartup = true)] TimerInfo myTimer, FunctionContext context)
    {
        _logger.LogInformation("AIRSIGMET Function executed at: {time}", DateTime.UtcNow);
        try
        {
            await _airsigmetService.PollWeatherDataAsync(context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred executing AIRSIGMET service");
            throw;
        }
    }
}
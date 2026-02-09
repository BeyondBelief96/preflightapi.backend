using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.ValueObjects.FaaPublications;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Azure.Functions.Functions;

public class FixFunction
{
    private readonly IFixCronService _fixService;
    private readonly IFaaPublicationCycleService _publicationService;
    private readonly ILogger<FixFunction> _logger;

    public FixFunction(
        IFixCronService fixService,
        IFaaPublicationCycleService publicationService,
        ILoggerFactory loggerFactory)
    {
        _fixService = fixService ?? throw new ArgumentNullException(nameof(fixService));
        _publicationService = publicationService ?? throw new ArgumentNullException(nameof(publicationService));
        _logger = loggerFactory.CreateLogger<FixFunction>();
    }

    [Function("FixFunction")]
    public async Task Run([TimerTrigger("0 0 3 * * *", RunOnStartup = false)] TimerInfo myTimer, FunctionContext context)
    {
        _logger.LogInformation("Fix Function executed at: {Time}", DateTime.UtcNow);
        var cancellationToken = context.CancellationToken;

        try
        {
            var currentDate = DateTime.UtcNow;

            if (await _publicationService.ShouldRunUpdateAsync(PublicationType.NasrSubscription_Fixes, currentDate))
            {
                var sw = Stopwatch.StartNew();
                _logger.LogInformation("Starting fix data update process");
                await _fixService.DownloadAndProcessDataAsync(cancellationToken);
                await _publicationService.UpdateLastSuccessfulRunAsync(PublicationType.NasrSubscription_Fixes, currentDate);
                _logger.LogInformation("Fix data update completed successfully in {ElapsedMs}ms", sw.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogInformation("No fix data update needed at this time");
            }
        }
        catch (Exception)
        {
            throw;
        }
    }
}

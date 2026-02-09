using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.ValueObjects.FaaPublications;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Azure.Functions.Functions;

public class NavigationalAidFunction
{
    private readonly INavigationalAidCronService _navAidService;
    private readonly IFaaPublicationCycleService _publicationService;
    private readonly ILogger<NavigationalAidFunction> _logger;

    public NavigationalAidFunction(
        INavigationalAidCronService navAidService,
        IFaaPublicationCycleService publicationService,
        ILoggerFactory loggerFactory)
    {
        _navAidService = navAidService ?? throw new ArgumentNullException(nameof(navAidService));
        _publicationService = publicationService ?? throw new ArgumentNullException(nameof(publicationService));
        _logger = loggerFactory.CreateLogger<NavigationalAidFunction>();
    }

    [Function("NavigationalAidFunction")]
    public async Task Run([TimerTrigger("0 0 2 * * *", RunOnStartup = false)] TimerInfo myTimer, FunctionContext context)
    {
        _logger.LogInformation("NavigationalAid Function executed at: {Time}", DateTime.UtcNow);
        var cancellationToken = context.CancellationToken;

        try
        {
            var currentDate = DateTime.UtcNow;

            if (await _publicationService.ShouldRunUpdateAsync(PublicationType.NasrSubscription_NavigationalAids, currentDate))
            {
                var sw = Stopwatch.StartNew();
                _logger.LogInformation("Starting navigational aid data update process");
                await _navAidService.DownloadAndProcessDataAsync(cancellationToken);
                await _publicationService.UpdateLastSuccessfulRunAsync(PublicationType.NasrSubscription_NavigationalAids, currentDate);
                _logger.LogInformation("Navigational aid data update completed successfully in {ElapsedMs}ms", sw.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogInformation("No navigational aid data update needed at this time");
            }
        }
        catch (Exception)
        {
            throw;
        }
    }
}

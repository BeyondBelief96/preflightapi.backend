using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.ValueObjects.FaaPublications;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Azure.Functions.Functions
{
    public class FrequencyFunction
    {
        private readonly ICommunicationFrequencyCronService _frequencyService;
        private readonly IFaaPublicationCycleService _publicationService;
        private readonly ILogger<FrequencyFunction> _logger;

        public FrequencyFunction(
            ICommunicationFrequencyCronService frequencyService,
            IFaaPublicationCycleService publicationService,
            ILoggerFactory loggerFactory)
        {
            _frequencyService = frequencyService ?? throw new ArgumentNullException(nameof(frequencyService));
            _publicationService = publicationService ?? throw new ArgumentNullException(nameof(publicationService));
            _logger = loggerFactory.CreateLogger<FrequencyFunction>();
        }

        [Function("FrequencyFunction")]
        [ExponentialBackoffRetry(5, "00:00:30", "00:15:00")]
        public async Task Run([TimerTrigger("0 30 10 * * *", RunOnStartup = false)] TimerInfo myTimer, FunctionContext context)
        {
            _logger.LogInformation("Frequency Function executed at: {Time}", DateTime.UtcNow);
            var cancellationToken = context.CancellationToken;

            var currentDate = DateTime.UtcNow;

            if (await _publicationService.ShouldRunUpdateAsync(PublicationType.NasrSubscription_Frequencies, currentDate))
            {
                var sw = Stopwatch.StartNew();
                _logger.LogInformation("Starting frequency data update process");
                await _frequencyService.DownloadAndProcessDataAsync(cancellationToken);
                await _publicationService.UpdateLastSuccessfulRunAsync(PublicationType.NasrSubscription_Frequencies, currentDate);
                _logger.LogInformation("Frequency data update completed successfully in {ElapsedMs}ms", sw.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogInformation("No frequency data update needed at this time");
            }
        }
    }
}
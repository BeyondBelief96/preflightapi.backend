using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.ValueObjects.FaaPublications;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Azure.Functions.Functions
{
    public class ChartSupplementFunction
    {
        private readonly IChartSupplementCronService _chartSupplementService;
        private readonly IFaaPublicationCycleService _publicationService;
        private readonly ILogger<ChartSupplementFunction> _logger;

        public ChartSupplementFunction(
            IChartSupplementCronService chartSupplementService,
            IFaaPublicationCycleService publicationService,
            ILoggerFactory loggerFactory)
        {
            _chartSupplementService = chartSupplementService ?? throw new ArgumentNullException(nameof(chartSupplementService));
            _publicationService = publicationService ?? throw new ArgumentNullException(nameof(publicationService));
            _logger = loggerFactory.CreateLogger<ChartSupplementFunction>();
        }

        [Function("ChartSupplementFunction")]
        [ExponentialBackoffRetry(5, "00:00:30", "00:15:00")]
        public async Task Run([TimerTrigger("0 0 12 * * *", RunOnStartup = false)] TimerInfo myTimer, FunctionContext context)
        {
            _logger.LogInformation("Chart Supplement Function executed at: {Time}", DateTime.UtcNow);
            var cancellationToken = context.CancellationToken;

            var currentDate = DateTime.UtcNow;

            if (await _publicationService.ShouldRunUpdateAsync(PublicationType.ChartSupplement, currentDate))
            {
                var sw = Stopwatch.StartNew();
                _logger.LogInformation("Starting chart supplement update process");
                await _chartSupplementService.DownloadAndProcessChartSupplementsAsync(cancellationToken);
                await _publicationService.UpdateLastSuccessfulRunAsync(PublicationType.ChartSupplement, currentDate);
                _logger.LogInformation("Chart supplement update completed successfully in {ElapsedMs}ms", sw.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogInformation("No chart supplement update needed at this time");
            }
        }
    }
}
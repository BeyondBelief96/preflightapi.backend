using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Constants;
using PreflightApi.Domain.ValueObjects.FaaPublications;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Azure.Functions.Functions
{
    public class NavaidFunction
    {
        private readonly INavaidCronService _navaidService;
        private readonly IFaaPublicationCycleService _publicationService;
        private readonly IDataSyncStatusService _syncStatusService;
        private readonly ILogger<NavaidFunction> _logger;

        public NavaidFunction(
            INavaidCronService navaidService,
            IFaaPublicationCycleService publicationService,
            IDataSyncStatusService syncStatusService,
            ILoggerFactory loggerFactory)
        {
            _navaidService = navaidService ?? throw new ArgumentNullException(nameof(navaidService));
            _publicationService = publicationService ?? throw new ArgumentNullException(nameof(publicationService));
            _syncStatusService = syncStatusService ?? throw new ArgumentNullException(nameof(syncStatusService));
            _logger = loggerFactory.CreateLogger<NavaidFunction>();
        }

        [Function("NavaidFunction")]
        [ExponentialBackoffRetry(5, "00:00:30", "00:15:00")]
        public async Task Run([TimerTrigger("0 45 10 * * *", RunOnStartup = true)] TimerInfo myTimer, FunctionContext context)
        {
            _logger.LogInformation("Navaid Function executed at: {Time}", DateTime.UtcNow);
            var cancellationToken = context.CancellationToken;

            var currentDate = DateTime.UtcNow;

            if (await _publicationService.ShouldRunUpdateAsync(PublicationType.NasrSubscription_Navaids, currentDate))
            {
                var sw = Stopwatch.StartNew();
                _logger.LogInformation("Starting NAVAID data update process");
                try
                {
                    await _navaidService.DownloadAndProcessDataAsync(cancellationToken);
                    await _navaidService.ProcessCheckpointsAndRemarksAsync(cancellationToken);
                    await _publicationService.UpdateLastSuccessfulRunAsync(PublicationType.NasrSubscription_Navaids, currentDate);
                    await _syncStatusService.RecordSuccessAsync(SyncTypes.Navaid, ct: cancellationToken);
                    _logger.LogInformation("NAVAID data update completed successfully in {ElapsedMs}ms", sw.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    try { await _syncStatusService.RecordFailureAsync(SyncTypes.Navaid, ex.Message, cancellationToken); }
                    catch (Exception inner) { _logger.LogWarning(inner, "Failed to record sync failure for Navaid"); }
                    throw;
                }
            }
            else
            {
                _logger.LogInformation("No NAVAID data update needed at this time");
            }
        }
    }
}

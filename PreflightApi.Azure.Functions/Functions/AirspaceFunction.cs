using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Constants;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.ValueObjects.FaaPublications;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Azure.Functions.Functions
{
    public class AirspaceFunction
    {
        private readonly IAirspaceCronService<Airspace> _airspaceService;
        private readonly IFaaPublicationCycleService _publicationService;
        private readonly IDataSyncStatusService _syncStatusService;
        private readonly ILogger<AirspaceFunction> _logger;

        public AirspaceFunction(
            IAirspaceCronService<Airspace> airspaceService,
            IFaaPublicationCycleService publicationService,
            IDataSyncStatusService syncStatusService,
            ILoggerFactory loggerFactory)
        {
            _airspaceService = airspaceService ?? throw new ArgumentNullException(nameof(airspaceService));
            _publicationService = publicationService ?? throw new ArgumentNullException(nameof(publicationService));
            _syncStatusService = syncStatusService ?? throw new ArgumentNullException(nameof(syncStatusService));
            _logger = loggerFactory.CreateLogger<AirspaceFunction>();
        }

        [Function("AirspaceFunction")]
        [ExponentialBackoffRetry(5, "00:00:30", "00:15:00")]
        public async Task Run([TimerTrigger("0 0 11 * * *", RunOnStartup = FunctionDefaults.RunOnStartup)] TimerInfo myTimer, FunctionContext context)
        {
            _logger.LogInformation("Airspace Function executed at: {Time}", DateTime.UtcNow);
            var cancellationToken = context.CancellationToken;

            var currentDate = DateTime.UtcNow;

            if (await _publicationService.ShouldRunUpdateAsync(PublicationType.Airspaces, currentDate))
            {
                var sw = Stopwatch.StartNew();
                _logger.LogInformation("Starting airspace update process");
                try
                {
                    await _airspaceService.UpdateAirspacesAsync(cancellationToken);
                    await _publicationService.UpdateLastSuccessfulRunAsync(PublicationType.Airspaces, currentDate);
                    await _syncStatusService.RecordSuccessAsync(SyncTypes.Airspace, ct: cancellationToken);
                    _logger.LogInformation("Airspace update completed successfully in {ElapsedMs}ms", sw.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    try { await _syncStatusService.RecordFailureAsync(SyncTypes.Airspace, ex.Message, cancellationToken); }
                    catch (Exception inner) { _logger.LogWarning(inner, "Failed to record sync failure for Airspace"); }
                    throw;
                }
            }
            else
            {
                _logger.LogInformation("No airspace update needed at this time");
            }
        }
    }
}

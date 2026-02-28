using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Constants;
using PreflightApi.Domain.ValueObjects.FaaPublications;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Azure.Functions.Functions
{
    public class AirportFunction
    {
        private readonly IAirportCronService _airportService;
        private readonly IRunwayCronService _runwayService;
        private readonly IRunwayEndCronService _runwayEndService;
        private readonly IRunwayGeometryCronService _runwayGeometryService;
        private readonly IFaaPublicationCycleService _publicationService;
        private readonly IDataSyncStatusService _syncStatusService;
        private readonly ILogger<AirportFunction> _logger;

        public AirportFunction(
            IAirportCronService airportService,
            IRunwayCronService runwayService,
            IRunwayEndCronService runwayEndService,
            IRunwayGeometryCronService runwayGeometryService,
            IFaaPublicationCycleService publicationService,
            IDataSyncStatusService syncStatusService,
            ILoggerFactory loggerFactory)
        {
            _airportService = airportService ?? throw new ArgumentNullException(nameof(airportService));
            _runwayService = runwayService ?? throw new ArgumentNullException(nameof(runwayService));
            _runwayEndService = runwayEndService ?? throw new ArgumentNullException(nameof(runwayEndService));
            _runwayGeometryService = runwayGeometryService ?? throw new ArgumentNullException(nameof(runwayGeometryService));
            _publicationService = publicationService ?? throw new ArgumentNullException(nameof(publicationService));
            _syncStatusService = syncStatusService ?? throw new ArgumentNullException(nameof(syncStatusService));
            _logger = loggerFactory.CreateLogger<AirportFunction>();
        }

        [Function("AirportFunction")]
        [ExponentialBackoffRetry(5, "00:00:30", "00:15:00")]
        public async Task Run([TimerTrigger("0 0 10 * * *", RunOnStartup = false)] TimerInfo myTimer, FunctionContext context)
        {
            _logger.LogInformation("Airport Function executed at: {Time}", DateTime.UtcNow);
            var cancellationToken = context.CancellationToken;

            var currentDate = DateTime.UtcNow;

            if (await _publicationService.ShouldRunUpdateAsync(PublicationType.NasrSubscription_Airport, currentDate))
            {
                var sw = Stopwatch.StartNew();
                _logger.LogInformation("Starting airport data update process");
                try
                {
                    // Process airports first (APT_BASE.csv, APT_ATT.csv, APT_CON.csv)
                    await _airportService.DownloadAndProcessDataAsync(cancellationToken);
                    _logger.LogInformation("Airport base data processing completed");

                    // Process runways (APT_RWY.csv)
                    await _runwayService.DownloadAndProcessDataAsync(cancellationToken);
                    _logger.LogInformation("Runway data processing completed");

                    // Process runway ends (APT_RWY_END.csv)
                    await _runwayEndService.DownloadAndProcessDataAsync(cancellationToken);
                    _logger.LogInformation("Runway end data processing completed");

                    // Link runway ends to their parent runways
                    await _runwayEndService.LinkRunwayEndsToRunwaysAsync(cancellationToken);
                    _logger.LogInformation("Runway end linking completed");

                    // Update runway polygon geometry from ArcGIS (non-critical — failure is logged but does not fail the sync)
                    try
                    {
                        await _runwayGeometryService.UpdateRunwayGeometriesAsync(cancellationToken);
                        await _syncStatusService.RecordSuccessAsync(SyncTypes.RunwayGeometry, ct: cancellationToken);
                        _logger.LogInformation("Runway geometry sync completed");
                    }
                    catch (Exception geoEx)
                    {
                        _logger.LogWarning(geoEx, "Runway geometry sync failed — airport/runway data was still updated successfully");
                        try { await _syncStatusService.RecordFailureAsync(SyncTypes.RunwayGeometry, geoEx.Message, cancellationToken); }
                        catch (Exception inner) { _logger.LogWarning(inner, "Failed to record sync failure for RunwayGeometry"); }
                    }

                    await _publicationService.UpdateLastSuccessfulRunAsync(PublicationType.NasrSubscription_Airport, currentDate);
                    await _syncStatusService.RecordSuccessAsync(SyncTypes.Airport, ct: cancellationToken);
                    _logger.LogInformation("Airport data update completed successfully in {ElapsedMs}ms", sw.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    try { await _syncStatusService.RecordFailureAsync(SyncTypes.Airport, ex.Message, cancellationToken); }
                    catch (Exception inner) { _logger.LogWarning(inner, "Failed to record sync failure for Airport"); }
                    throw;
                }
            }
            else
            {
                _logger.LogInformation("No airport data update needed at this time");
            }
        }
    }
}

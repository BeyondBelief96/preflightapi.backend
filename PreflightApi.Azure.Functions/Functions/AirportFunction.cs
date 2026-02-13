using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.ValueObjects.FaaPublications;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Azure.Functions.Functions
{
    public class AirportFunction
    {
        private readonly IAirportCronService _airportService;
        private readonly IRunwayCronService _runwayService;
        private readonly IRunwayEndCronService _runwayEndService;
        private readonly IFaaPublicationCycleService _publicationService;
        private readonly ILogger<AirportFunction> _logger;

        public AirportFunction(
            IAirportCronService airportService,
            IRunwayCronService runwayService,
            IRunwayEndCronService runwayEndService,
            IFaaPublicationCycleService publicationService,
            ILoggerFactory loggerFactory)
        {
            _airportService = airportService ?? throw new ArgumentNullException(nameof(airportService));
            _runwayService = runwayService ?? throw new ArgumentNullException(nameof(runwayService));
            _runwayEndService = runwayEndService ?? throw new ArgumentNullException(nameof(runwayEndService));
            _publicationService = publicationService ?? throw new ArgumentNullException(nameof(publicationService));
            _logger = loggerFactory.CreateLogger<AirportFunction>();
        }

        [Function("AirportFunction")]
        [ExponentialBackoffRetry(5, "00:00:30", "00:15:00")]
        public async Task Run([TimerTrigger("0 0 0 * * *", RunOnStartup = false)] TimerInfo myTimer, FunctionContext context)
        {
            _logger.LogInformation("Airport Function executed at: {Time}", DateTime.UtcNow);
            var cancellationToken = context.CancellationToken;

            try
            {
                var currentDate = DateTime.UtcNow;

                if (await _publicationService.ShouldRunUpdateAsync(PublicationType.NasrSubscription_Airport, currentDate))
                {
                    var sw = Stopwatch.StartNew();
                    _logger.LogInformation("Starting airport data update process");

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

                    await _publicationService.UpdateLastSuccessfulRunAsync(PublicationType.NasrSubscription_Airport, currentDate);
                    _logger.LogInformation("Airport data update completed successfully in {ElapsedMs}ms", sw.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogInformation("No airport data update needed at this time");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}

using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.ValueObjects.FaaPublications;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Azure.Functions.Functions
{
    public class AirspaceFunction
    {
        private readonly IAirspaceCronService<Airspace> _airspaceService;
        private readonly IFaaPublicationCycleService _publicationService;
        private readonly ILogger<AirspaceFunction> _logger;

        public AirspaceFunction(
            IAirspaceCronService<Airspace> airspaceService,
            IFaaPublicationCycleService publicationService,
            ILoggerFactory loggerFactory)
        {
            _airspaceService = airspaceService ?? throw new ArgumentNullException(nameof(airspaceService));
            _publicationService = publicationService ?? throw new ArgumentNullException(nameof(publicationService));
            _logger = loggerFactory.CreateLogger<AirspaceFunction>();
        }

        [Function("AirspaceFunction")]
        [ExponentialBackoffRetry(5, "00:00:30", "00:15:00")]
        public async Task Run([TimerTrigger("0 0 11 * * *", RunOnStartup = false)] TimerInfo myTimer, FunctionContext context)
        {
            _logger.LogInformation("Airspace Function executed at: {Time}", DateTime.UtcNow);
            var cancellationToken = context.CancellationToken;

            var currentDate = DateTime.UtcNow;

            if (await _publicationService.ShouldRunUpdateAsync(PublicationType.Airspaces, currentDate))
            {
                var sw = Stopwatch.StartNew();
                _logger.LogInformation("Starting airspace update process");
                await _airspaceService.UpdateAirspacesAsync(cancellationToken);
                await _publicationService.UpdateLastSuccessfulRunAsync(PublicationType.Airspaces, currentDate);
                _logger.LogInformation("Airspace update completed successfully in {ElapsedMs}ms", sw.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogInformation("No airspace update needed at this time");
            }
        }
    }
}
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
        public async Task Run([TimerTrigger("0 0 2 * * *")] TimerInfo myTimer, FunctionContext context)
        {
            _logger.LogInformation($"Airspace Function executed at: {DateTime.UtcNow}");
            var cancellationToken = context.CancellationToken;

            try
            {
                var currentDate = DateTime.UtcNow;

                if (await _publicationService.ShouldRunUpdateAsync(PublicationType.Airspaces, currentDate))
                {
                    _logger.LogInformation("Starting airspace update process");
                    await _airspaceService.UpdateAirspacesAsync(cancellationToken);
                    await _publicationService.UpdateLastSuccessfulRunAsync(PublicationType.Airspaces, currentDate);
                    _logger.LogInformation("Airspace update completed successfully");
                }
                else
                {
                    _logger.LogInformation("No airspace update needed at this time");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing airspace update job");
                throw;
            }
        }
    }
}
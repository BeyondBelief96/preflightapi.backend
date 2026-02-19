using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.ValueObjects.FaaPublications;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Azure.Functions.Functions
{
    public class AirportDiagramFunction
    {
        private readonly IAirportDiagramCronService _airportDiagramService;
        private readonly IFaaPublicationCycleService _publicationService;
        private readonly ILogger<AirportDiagramFunction> _logger;

        public AirportDiagramFunction(
            IAirportDiagramCronService airportDiagramService,
            IFaaPublicationCycleService publicationService,
            ILoggerFactory loggerFactory)
        {
            _airportDiagramService = airportDiagramService ?? throw new ArgumentNullException(nameof(airportDiagramService));
            _publicationService = publicationService ?? throw new ArgumentNullException(nameof(publicationService));
            _logger = loggerFactory.CreateLogger<AirportDiagramFunction>();
        }

        [Function("AirportDiagramFunction")]
        [ExponentialBackoffRetry(5, "00:00:30", "00:15:00")]
        public async Task Run([TimerTrigger("0 30 12 * * *", RunOnStartup = false)] TimerInfo myTimer, FunctionContext context)
        {
            _logger.LogInformation("Airport Diagram Function executed at: {Time}", DateTime.UtcNow);
            var cancellationToken = context.CancellationToken;

            try
            {
                var currentDate = DateTime.UtcNow;

                if (await _publicationService.ShouldRunUpdateAsync(PublicationType.AirportDiagram, currentDate))
                {
                    var sw = Stopwatch.StartNew();
                    _logger.LogInformation("Starting airport diagram update process");
                    await _airportDiagramService.DownloadAndProcessAirportDiagramsAsync(cancellationToken);
                    await _publicationService.UpdateLastSuccessfulRunAsync(PublicationType.AirportDiagram, currentDate);
                    _logger.LogInformation("Airport diagram update completed successfully in {ElapsedMs}ms", sw.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogInformation("No airport diagram update needed at this time");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
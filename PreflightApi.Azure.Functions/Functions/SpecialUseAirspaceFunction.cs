using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.ValueObjects.FaaPublications;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Azure.Functions.Functions
{
    public class SpecialUseAirspaceFunction
    {
        private readonly IAirspaceCronService<SpecialUseAirspace> _specialUseAirspaceService;
        private readonly IFaaPublicationCycleService _publicationService;
        private readonly ILogger<SpecialUseAirspaceFunction> _logger;

        public SpecialUseAirspaceFunction(
            IAirspaceCronService<SpecialUseAirspace> specialUseAirspaceService,
            IFaaPublicationCycleService publicationService,
            ILoggerFactory loggerFactory)
        {
            _specialUseAirspaceService = specialUseAirspaceService ?? throw new ArgumentNullException(nameof(specialUseAirspaceService));
            _publicationService = publicationService ?? throw new ArgumentNullException(nameof(publicationService));
            _logger = loggerFactory.CreateLogger<SpecialUseAirspaceFunction>();
        }

        [Function("SpecialUseAirspaceFunction")]
        [ExponentialBackoffRetry(5, "00:00:30", "00:15:00")]
        public async Task Run([TimerTrigger("0 0 3 * * *", RunOnStartup = false)] TimerInfo myTimer, FunctionContext context)
        {
            _logger.LogInformation("Special Use Airspace Function executed at: {Time}", DateTime.UtcNow);
            var cancellationToken = context.CancellationToken;

            try
            {
                var currentDate = DateTime.UtcNow;

                if (await _publicationService.ShouldRunUpdateAsync(PublicationType.SpecialUseAirspaces, currentDate))
                {
                    var sw = Stopwatch.StartNew();
                    _logger.LogInformation("Starting special use airspace update process");
                    await _specialUseAirspaceService.UpdateAirspacesAsync(cancellationToken);
                    await _publicationService.UpdateLastSuccessfulRunAsync(PublicationType.SpecialUseAirspaces, currentDate);
                    _logger.LogInformation("Special use airspace update completed successfully in {ElapsedMs}ms", sw.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogInformation("No special use airspace update needed at this time");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
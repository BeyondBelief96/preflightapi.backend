using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.ValueObjects.FaaPublications;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Azure.Functions.Functions
{
    public class TerminalProcedureFunction
    {
        private readonly ITerminalProcedureCronService _terminalProcedureService;
        private readonly IFaaPublicationCycleService _publicationService;
        private readonly ILogger<TerminalProcedureFunction> _logger;

        public TerminalProcedureFunction(
            ITerminalProcedureCronService terminalProcedureService,
            IFaaPublicationCycleService publicationService,
            ILoggerFactory loggerFactory)
        {
            _terminalProcedureService = terminalProcedureService ?? throw new ArgumentNullException(nameof(terminalProcedureService));
            _publicationService = publicationService ?? throw new ArgumentNullException(nameof(publicationService));
            _logger = loggerFactory.CreateLogger<TerminalProcedureFunction>();
        }

        [Function("TerminalProcedureFunction")]
        [ExponentialBackoffRetry(5, "00:00:30", "00:15:00")]
        public async Task Run([TimerTrigger("0 30 12 * * *", RunOnStartup = false)] TimerInfo myTimer, FunctionContext context)
        {
            _logger.LogInformation("Terminal Procedure Function executed at: {Time}", DateTime.UtcNow);
            var cancellationToken = context.CancellationToken;

            var currentDate = DateTime.UtcNow;

            if (await _publicationService.ShouldRunUpdateAsync(PublicationType.TerminalProcedure, currentDate))
            {
                var sw = Stopwatch.StartNew();
                _logger.LogInformation("Starting terminal procedure update process");
                await _terminalProcedureService.DownloadAndProcessTerminalProceduresAsync(cancellationToken);
                await _publicationService.UpdateLastSuccessfulRunAsync(PublicationType.TerminalProcedure, currentDate);
                _logger.LogInformation("Terminal procedure update completed successfully in {ElapsedMs}ms", sw.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogInformation("No terminal procedure update needed at this time");
            }
        }
    }
}

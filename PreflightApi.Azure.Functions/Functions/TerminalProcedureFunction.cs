using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Constants;
using PreflightApi.Domain.ValueObjects.FaaPublications;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Azure.Functions.Functions
{
    public class TerminalProcedureFunction
    {
        private readonly ITerminalProcedureCronService _terminalProcedureService;
        private readonly IFaaPublicationCycleService _publicationService;
        private readonly IDataSyncStatusService _syncStatusService;
        private readonly ILogger<TerminalProcedureFunction> _logger;

        public TerminalProcedureFunction(
            ITerminalProcedureCronService terminalProcedureService,
            IFaaPublicationCycleService publicationService,
            IDataSyncStatusService syncStatusService,
            ILoggerFactory loggerFactory)
        {
            _terminalProcedureService = terminalProcedureService ?? throw new ArgumentNullException(nameof(terminalProcedureService));
            _publicationService = publicationService ?? throw new ArgumentNullException(nameof(publicationService));
            _syncStatusService = syncStatusService ?? throw new ArgumentNullException(nameof(syncStatusService));
            _logger = loggerFactory.CreateLogger<TerminalProcedureFunction>();
        }

        [Function("TerminalProcedureFunction")]
        [ExponentialBackoffRetry(5, "00:00:30", "00:15:00")]
        public async Task Run([TimerTrigger("0 30 12 * * *", RunOnStartup = true)] TimerInfo myTimer, FunctionContext context)
        {
            _logger.LogInformation("Terminal Procedure Function executed at: {Time}", DateTime.UtcNow);
            var cancellationToken = context.CancellationToken;

            var currentDate = DateTime.UtcNow;

            if (await _publicationService.ShouldRunUpdateAsync(PublicationType.TerminalProcedure, currentDate))
            {
                var sw = Stopwatch.StartNew();
                _logger.LogInformation("Starting terminal procedure update process");
                try
                {
                    await _terminalProcedureService.DownloadAndProcessTerminalProceduresAsync(cancellationToken);
                    await _publicationService.UpdateLastSuccessfulRunAsync(PublicationType.TerminalProcedure, currentDate);
                    await _syncStatusService.RecordSuccessAsync(SyncTypes.TerminalProcedure, ct: cancellationToken);
                    _logger.LogInformation("Terminal procedure update completed successfully in {ElapsedMs}ms", sw.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    try { await _syncStatusService.RecordFailureAsync(SyncTypes.TerminalProcedure, ex.Message, cancellationToken); }
                    catch (Exception inner) { _logger.LogWarning(inner, "Failed to record sync failure for TerminalProcedure"); }
                    throw;
                }
            }
            else
            {
                _logger.LogInformation("No terminal procedure update needed at this time");
            }
        }
    }
}

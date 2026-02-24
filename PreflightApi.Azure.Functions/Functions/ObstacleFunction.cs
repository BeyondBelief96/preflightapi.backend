using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Constants;
using PreflightApi.Domain.ValueObjects.FaaPublications;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Azure.Functions.Functions;

public class ObstacleFunction
{
    private readonly IObstacleCronService _obstacleService;
    private readonly IFaaPublicationCycleService _publicationService;
    private readonly IDataSyncStatusService _syncStatusService;
    private readonly ILogger<ObstacleFunction> _logger;

    public ObstacleFunction(
        IObstacleCronService obstacleService,
        IFaaPublicationCycleService publicationService,
        IDataSyncStatusService syncStatusService,
        ILoggerFactory loggerFactory)
    {
        _obstacleService = obstacleService ?? throw new ArgumentNullException(nameof(obstacleService));
        _publicationService = publicationService ?? throw new ArgumentNullException(nameof(publicationService));
        _syncStatusService = syncStatusService ?? throw new ArgumentNullException(nameof(syncStatusService));
        _logger = loggerFactory.CreateLogger<ObstacleFunction>();
    }

    [Function("ObstacleFunction")]
    [ExponentialBackoffRetry(5, "00:00:30", "00:15:00")]
    public async Task Run([TimerTrigger("0 0 12 * * *", RunOnStartup = false)] TimerInfo myTimer, FunctionContext context)
    {
        _logger.LogInformation("Obstacle Function executed at: {Time}", DateTime.UtcNow);
        var cancellationToken = context.CancellationToken;

        var currentDate = DateTime.UtcNow;

        if (await _publicationService.ShouldRunUpdateAsync(PublicationType.Obstacles, currentDate))
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("Starting obstacle data update process");
            try
            {
                await _obstacleService.DownloadAndProcessObstaclesAsync(cancellationToken);
                await _publicationService.UpdateLastSuccessfulRunAsync(PublicationType.Obstacles, currentDate);
                await _syncStatusService.RecordSuccessAsync(SyncTypes.Obstacle, ct: cancellationToken);
                _logger.LogInformation("Obstacle data update completed successfully in {ElapsedMs}ms", sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                try { await _syncStatusService.RecordFailureAsync(SyncTypes.Obstacle, ex.Message, cancellationToken); }
                catch (Exception inner) { _logger.LogWarning(inner, "Failed to record sync failure for Obstacle"); }
                throw;
            }
        }
        else
        {
            _logger.LogInformation("No obstacle data update needed at this time");
        }
    }
}

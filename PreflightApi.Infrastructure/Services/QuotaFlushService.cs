using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Infrastructure.Services;

public class QuotaFlushService : BackgroundService
{
    private readonly IQuotaTrackingService _quotaTrackingService;
    private readonly ILogger<QuotaFlushService> _logger;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(30);

    public QuotaFlushService(IQuotaTrackingService quotaTrackingService, ILogger<QuotaFlushService> logger)
    {
        _quotaTrackingService = quotaTrackingService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Quota flush service started (interval: {Interval}s)", FlushInterval.TotalSeconds);

        using var timer = new PeriodicTimer(FlushInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await _quotaTrackingService.FlushToDatabaseAsync(stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Quota flush service stopping, performing final flush");
        await _quotaTrackingService.FlushToDatabaseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}

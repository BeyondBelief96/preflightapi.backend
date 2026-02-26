using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PreflightApi.Infrastructure.HealthChecks;

public class HealthMonitorService : BackgroundService
{
    private readonly HealthCheckService _healthCheckService;
    private readonly HealthSnapshotStore _store;
    private readonly TimeSpan _interval;
    private readonly ILogger<HealthMonitorService> _logger;

    public HealthMonitorService(
        HealthCheckService healthCheckService,
        HealthSnapshotStore store,
        IOptions<HealthMonitorSettings> settings,
        ILogger<HealthMonitorService> logger)
    {
        _healthCheckService = healthCheckService;
        _store = store;
        _interval = TimeSpan.FromSeconds(settings.Value.IntervalSeconds);
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run first check immediately so the snapshot is populated before HTTP requests arrive
        await RunCheckAsync(stoppingToken);

        using var timer = new PeriodicTimer(_interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCheckAsync(stoppingToken);
        }
    }

    private async Task RunCheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            var report = await _healthCheckService.CheckHealthAsync(cancellationToken);
            _store.Update(report);
            _logger.LogDebug("Health check cycle completed: {Status}", report.Status);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Health check cycle failed");
        }
    }
}

using Microsoft.Extensions.Diagnostics.HealthChecks;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Infrastructure.HealthChecks;

public class DataFreshnessHealthCheck : IHealthCheck
{
    private readonly IDataSyncStatusService _dataSyncStatusService;

    public DataFreshnessHealthCheck(IDataSyncStatusService dataSyncStatusService)
    {
        _dataSyncStatusService = dataSyncStatusService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await _dataSyncStatusService.GetAllFreshnessAsync(cancellationToken);
            var staleTypes = results.Where(r => !r.IsFresh).ToList();

            if (staleTypes.Count == 0)
            {
                return HealthCheckResult.Healthy("All data sources are fresh.");
            }

            var description = string.Join("; ", staleTypes.Select(s => s.Message));
            var data = new Dictionary<string, object>();
            foreach (var stale in staleTypes)
            {
                data[stale.SyncType] = new { stale.Severity, stale.Message };
            }

            return HealthCheckResult.Degraded(description, data: data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("Unable to evaluate data freshness.", ex);
        }
    }
}

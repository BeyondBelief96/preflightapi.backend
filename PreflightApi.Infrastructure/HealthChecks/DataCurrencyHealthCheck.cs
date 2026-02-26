using Microsoft.Extensions.Diagnostics.HealthChecks;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Infrastructure.HealthChecks;

public class DataCurrencyHealthCheck : IHealthCheck
{
    private readonly IDataSyncStatusService _dataSyncStatusService;

    public DataCurrencyHealthCheck(IDataSyncStatusService dataSyncStatusService)
    {
        _dataSyncStatusService = dataSyncStatusService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await _dataSyncStatusService.GetAllCurrencyAsync(cancellationToken);
            var staleTypes = results.Where(r => !r.IsFresh).ToList();

            if (staleTypes.Count == 0)
            {
                return HealthCheckResult.Healthy("All data sources are fresh.");
            }

            var noun = staleTypes.Count == 1 ? "data source is" : "data sources are";
            var description = $"{staleTypes.Count} {noun} stale — see Data Currency below for details.";

            return HealthCheckResult.Degraded(description);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("Unable to evaluate data currency.", ex);
        }
    }
}

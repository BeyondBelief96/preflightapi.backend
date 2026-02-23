using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Settings;

namespace PreflightApi.Infrastructure.HealthChecks;

/// <summary>
/// Health check that verifies Azure Blob Storage connectivity by checking container existence.
/// </summary>
public class BlobStorageHealthCheck : IHealthCheck
{
    private readonly ICloudStorageService _cloudStorageService;
    private readonly string _containerName;

    public BlobStorageHealthCheck(
        ICloudStorageService cloudStorageService,
        IOptions<CloudStorageSettings> settings)
    {
        _cloudStorageService = cloudStorageService;
        _containerName = settings.Value.ChartSupplementsContainerName;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _cloudStorageService.ContainerExistsAsync(_containerName);
            return exists
                ? HealthCheckResult.Healthy("Azure Blob Storage is reachable.")
                : HealthCheckResult.Degraded($"Container '{_containerName}' does not exist.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("Azure Blob Storage is unreachable.", ex);
        }
    }
}

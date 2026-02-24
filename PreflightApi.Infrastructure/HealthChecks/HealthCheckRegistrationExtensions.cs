using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PreflightApi.Infrastructure.HealthChecks;

/// <summary>
/// Extension methods for registering infrastructure health checks.
/// </summary>
public static class HealthCheckRegistrationExtensions
{
    public static IHealthChecksBuilder AddInfrastructureHealthChecks(this IHealthChecksBuilder builder)
    {
        builder.AddCheck<BlobStorageHealthCheck>(
            "blob-storage",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "ready" },
            timeout: TimeSpan.FromSeconds(5));

        builder.AddCheck<NoaaWeatherHealthCheck>(
            "noaa-weather",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "external" },
            timeout: TimeSpan.FromSeconds(5));

        builder.AddCheck<NoaaMagVarHealthCheck>(
            "noaa-magvar",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "external" },
            timeout: TimeSpan.FromSeconds(5));

        builder.AddCheck<DataFreshnessHealthCheck>(
            "data-freshness",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "ready", "data-freshness" },
            timeout: TimeSpan.FromSeconds(10));

        return builder;
    }
}

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using PreflightApi.Infrastructure.Settings;

namespace PreflightApi.Infrastructure.HealthChecks;

/// <summary>
/// Health check that verifies FAA NOTAM Management Service (NMS) API availability.
/// Probes the auth base URL — any HTTP response proves the service is reachable.
/// </summary>
public class FaaNmsHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _healthCheckUrl;

    public FaaNmsHealthCheck(
        IHttpClientFactory httpClientFactory,
        IOptions<NmsSettings> settings)
    {
        _httpClientFactory = httpClientFactory;
        _healthCheckUrl = settings.Value.AuthBaseUrl.TrimEnd('/');
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Head, _healthCheckUrl);
            using var response = await client.SendAsync(request, cancellationToken);

            return HealthCheckResult.Healthy($"FAA NMS API is reachable (HTTP {(int)response.StatusCode}).");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("FAA NMS API is unreachable.", ex);
        }
    }
}

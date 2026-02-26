using Microsoft.Extensions.Diagnostics.HealthChecks;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.HealthChecks;

/// <summary>
/// Health check that verifies NOAA Aviation Weather API availability.
/// </summary>
public class NoaaWeatherHealthCheck : IHealthCheck
{
    private static readonly Uri HealthCheckUri =
        new("https://aviationweather.gov/data/cache/metars.cache.xml.gz");

    private readonly IHttpClientFactory _httpClientFactory;

    public NoaaWeatherHealthCheck(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(ServiceCollectionExtensions.HealthCheckHttpClient);
            using var request = new HttpRequestMessage(HttpMethod.Head, HealthCheckUri);
            using var response = await client.SendAsync(request, cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("NOAA Aviation Weather API is reachable.")
                : HealthCheckResult.Degraded($"NOAA Aviation Weather API returned {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("NOAA Aviation Weather API is unreachable.", ex);
        }
    }
}

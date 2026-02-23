using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using PreflightApi.Infrastructure.Settings;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.HealthChecks;

/// <summary>
/// Health check that verifies NOAA Geomagnetic Declination API availability.
/// </summary>
public class NoaaMagVarHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiKey;

    public NoaaMagVarHealthCheck(
        IHttpClientFactory httpClientFactory,
        IOptions<NOAASettings> settings)
    {
        _httpClientFactory = httpClientFactory;
        _apiKey = settings.Value.NOAAApiKey ?? string.Empty;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(ServiceCollectionExtensions.MagVarHttpClient);
            var url = $"https://www.ngdc.noaa.gov/geomag-web/calculators/calculateDeclination" +
                      $"?lat1=0&lon1=0&key={_apiKey}&resultFormat=json";
            using var response = await client.GetAsync(url, cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("NOAA Geomagnetic Declination API is reachable.")
                : HealthCheckResult.Degraded($"NOAA Geomagnetic Declination API returned {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("NOAA Geomagnetic Declination API is unreachable.", ex);
        }
    }
}

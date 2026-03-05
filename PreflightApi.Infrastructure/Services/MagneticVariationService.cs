using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos.Navlog;
using PreflightApi.Infrastructure.Settings;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.Services;

public class MagneticVariationService : IMagneticVariationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiKey;
    private readonly ILogger<MagneticVariationService> _logger;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    public MagneticVariationService(
        IHttpClientFactory httpClientFactory,
        IOptions<NOAASettings> apiKeys,
        ILogger<MagneticVariationService> logger,
        IMemoryCache cache)
    {
        _httpClientFactory = httpClientFactory;
        _apiKey = apiKeys.Value.NOAAApiKey ?? throw new ArgumentNullException(nameof(apiKeys));
        _logger = logger;
        _cache = cache;
    }

    public async Task<double> GetMagneticVariation(double latitude, double longitude, CancellationToken ct = default)
    {
        string cacheKey = $"{latitude},{longitude}";

        if (_cache.TryGetValue(cacheKey, out double cachedVariation))
        {
            return cachedVariation;
        }

        try
        {
            var client = _httpClientFactory.CreateClient(ServiceCollectionExtensions.MagVarHttpClient);
            var response = await client.GetAsync(
                $"https://www.ngdc.noaa.gov/geomag-web/calculators/calculateDeclination?" +
                $"lat1={latitude}&lon1={longitude}&key={_apiKey}&resultFormat=json", ct);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<MagneticVariationResponseDto>(content);

            if (result?.Result == null || result.Result.Length == 0)
            {
                throw new InvalidOperationException("Invalid response from NOAA API");
            }

            var declination = result.Result[0].Declination;

            // Cache the result
            _cache.Set(cacheKey, declination, CacheDuration);

            return declination;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting magnetic variation for lat: {Latitude}, lon: {Longitude}",
                latitude, longitude);
            throw new MagneticVariationServiceException(
                "The NOAA magnetic variation service is temporarily unavailable. Please try again later.", ex);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid API response getting magnetic variation for lat: {Latitude}, lon: {Longitude}",
                latitude, longitude);
            throw new MagneticVariationServiceException(
                "The NOAA magnetic variation service returned an invalid response.", ex);
        }
    }
}



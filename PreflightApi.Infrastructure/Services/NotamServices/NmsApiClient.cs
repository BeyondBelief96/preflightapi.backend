using System.IO.Compression;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PreflightApi.Infrastructure.Dtos.Notam;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Services.NotamServices.SchemaManifests;
using PreflightApi.Infrastructure.Settings;

namespace PreflightApi.Infrastructure.Services.NotamServices;

/// <summary>
/// Low-level FAA NMS API client with OAuth2 client credentials authentication
/// Registered as Singleton to persist OAuth2 token across requests
/// </summary>
public class NmsApiClient : INmsApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly NmsSettings _settings;
    private readonly ILogger<NmsApiClient> _logger;

    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _accessToken;
    private DateTime _tokenExpiresAt = DateTime.MinValue;
    private const int TokenRefreshBufferSeconds = 60;

    private bool _schemaValidated;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public NmsApiClient(
        IHttpClientFactory httpClientFactory,
        IOptions<NmsSettings> settings,
        ILogger<NmsApiClient> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<NotamDto>> GetNotamsByLocationAsync(string location, NotamFilterDto? filters = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new ArgumentException("Location cannot be null or empty", nameof(location));
        }

        _logger.LogInformation("Fetching NOTAMs for location: {Location}", location);

        var url = $"{_settings.BaseUrl}/v1/notams?location={Uri.EscapeDataString(location.ToUpperInvariant())}";
        url = AppendFilters(url, filters);

        return await ExecuteWithAuthAsync(url, ct);
    }

    public async Task<List<NotamDto>> GetNotamsByRadiusAsync(double lat, double lon, double radiusNm, NotamFilterDto? filters = null, CancellationToken ct = default)
    {
        if (radiusNm <= 0 || radiusNm > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(radiusNm), "Radius must be between 0 and 100 nautical miles");
        }

        _logger.LogInformation("Fetching NOTAMs within {Radius}nm of {Lat}, {Lon}", radiusNm, lat, lon);

        var url = $"{_settings.BaseUrl}/v1/notams?latitude={lat:F6}&longitude={lon:F6}&radius={radiusNm:F1}";
        url = AppendFilters(url, filters);

        return await ExecuteWithAuthAsync(url, ct);
    }

    public async Task<NotamDto?> GetNotamByNmsIdAsync(string nmsId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nmsId))
        {
            throw new ArgumentException("NMS ID cannot be null or empty", nameof(nmsId));
        }

        _logger.LogInformation("Fetching NOTAM by NMS ID: {NmsId}", nmsId);

        var url = $"{_settings.BaseUrl}/v1/notams?nmsId={Uri.EscapeDataString(nmsId)}";
        var notams = await ExecuteWithAuthAsync(url, ct);

        return notams.FirstOrDefault();
    }

    public async Task<List<NotamDto>> GetNotamsByLastUpdatedDateAsync(DateTime lastUpdatedDate, CancellationToken ct = default)
    {
        var isoDate = lastUpdatedDate.ToString("O");
        _logger.LogInformation("Fetching NOTAMs updated since {LastUpdatedDate}", isoDate);

        var url = $"{_settings.BaseUrl}/v1/notams?lastUpdatedDate={Uri.EscapeDataString(isoDate)}&nmsResponseFormat=GEOJSON";

        return await ExecuteWithAuthAsync(url, ct);
    }

    public async Task<List<NotamDto>> GetAllNotamsInitialLoadAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Fetching initial load of all NOTAMs via /v1/notams/il");

        // Step 1: Request content URL via /v1/notams/il?allowRedirect=false
        // Returns { "status": "Success", "data": { "url": "/nmsapi/v1/content/{token}" } }
        var url = $"{_settings.BaseUrl}/v1/notams/il?allowRedirect=false";
        var responseContent = await ExecuteRawWithAuthAsync(url, ct);

        // Step 2: Parse the response to extract the content URL
        using var doc = JsonDocument.Parse(responseContent);
        var root = doc.RootElement;

        string? contentPath = null;
        if (root.TryGetProperty("data", out var data) &&
            data.TryGetProperty("url", out var urlElement))
        {
            contentPath = urlElement.GetString();
        }

        if (string.IsNullOrEmpty(contentPath))
        {
            _logger.LogWarning("No content URL in initial load response, attempting direct parse");
            return ParseGeoJsonResponse(responseContent);
        }

        // Step 3: Download the compressed file from the content URL (requires same Bearer auth)
        // Content path format: "/nmsapi/v1/content/{token}" or "/v1/content/{token}"
        string contentUrl;
        if (contentPath.StartsWith("http"))
            contentUrl = contentPath;
        else if (contentPath.StartsWith("/nmsapi/"))
            contentUrl = $"{_settings.AuthBaseUrl}{contentPath}";
        else
            contentUrl = $"{_settings.BaseUrl}{contentPath}";

        _logger.LogInformation("Downloading bulk NOTAM data from {ContentUrl}", contentUrl);

        var bulkContent = await ExecuteRawWithAuthAsync(contentUrl, ct);

        _logger.LogInformation("Parsing initial load data ({ContentLength} chars)", bulkContent.Length);

        // Initial load returns AIXM XML (SOAP-wrapped), not GeoJSON
        var trimmed = bulkContent.TrimStart();
        if (trimmed.StartsWith("<?xml", StringComparison.Ordinal) ||
            trimmed.StartsWith("<soap:", StringComparison.Ordinal))
        {
            _logger.LogInformation("Detected AIXM XML format in initial load response");
            return AixmNotamParser.Parse(bulkContent, _logger);
        }

        return ParseGeoJsonResponse(bulkContent);
    }

    private static string AppendFilters(string url, NotamFilterDto? filters)
    {
        if (filters == null || !filters.HasFilters)
            return url;

        if (filters.Classification != null)
            url += $"&classification={Uri.EscapeDataString(filters.Classification)}";

        if (filters.Feature != null)
            url += $"&feature={Uri.EscapeDataString(filters.Feature)}";

        if (filters.FreeText != null)
            url += $"&freeText={Uri.EscapeDataString(filters.FreeText)}";

        if (filters.EffectiveStartDate != null && filters.EffectiveEndDate != null)
        {
            url += $"&effectiveStartDate={Uri.EscapeDataString(filters.EffectiveStartDate)}";
            url += $"&effectiveEndDate={Uri.EscapeDataString(filters.EffectiveEndDate)}";
        }

        return url;
    }

    private async Task<List<NotamDto>> ExecuteWithAuthAsync(string url, CancellationToken ct)
    {
        var responseContent = await ExecuteRawWithAuthAsync(url, ct);
        return ParseGeoJsonResponse(responseContent);
    }

    private async Task<string> ExecuteRawWithAuthAsync(string url, CancellationToken ct)
    {
        var token = await GetAccessTokenAsync(ct);
        var client = _httpClientFactory.CreateClient("NmsApi");

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("nmsResponseFormat", "GEOJSON");

        var response = await client.SendAsync(request, ct);

        // If we get 401, log the response body for debugging, then force token refresh and retry once
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Received 401 from {Url}. Response: {ErrorBody}", url, errorBody);

            token = await ForceRefreshTokenAsync(ct);

            request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Add("nmsResponseFormat", "GEOJSON");

            response = await client.SendAsync(request, ct);
        }

        response.EnsureSuccessStatusCode();

        // Always buffer bytes first — the NMS content endpoint returns GZip data
        // even with Content-Type: application/json, so we detect by magic bytes (0x1F 0x8B).
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);

        if (bytes.Length >= 2 && bytes[0] == 0x1F && bytes[1] == 0x8B)
        {
            _logger.LogDebug("Detected GZip magic bytes, decompressing response from {Url}", url);
            await using var ms = new MemoryStream(bytes);
            await using var gzipStream = new GZipStream(ms, CompressionMode.Decompress);
            using var reader = new StreamReader(gzipStream);
            return await reader.ReadToEndAsync(ct);
        }

        return Encoding.UTF8.GetString(bytes);
    }

    private List<NotamDto> ParseGeoJsonResponse(string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // NMS API returns response wrapped in { status, data: { geojson: [] } }
            if (root.TryGetProperty("data", out var data))
            {
                if (data.TryGetProperty("geojson", out var geojson))
                {
                    ValidateSchemaIfNeeded(geojson);

                    var notams = new List<NotamDto>();
                    foreach (var feature in geojson.EnumerateArray())
                    {
                        var notam = JsonSerializer.Deserialize<NotamDto>(feature.GetRawText(), JsonOptions);
                        if (notam != null)
                        {
                            notams.Add(notam);
                        }
                    }
                    return notams;
                }

                // data.aixm format: array of AIXM XML strings, each containing one AIXMBasicMessage
                if (data.TryGetProperty("aixm", out var aixm) && aixm.ValueKind == JsonValueKind.Array)
                {
                    _logger.LogInformation("Parsing data.aixm response ({Count} items)", aixm.GetArrayLength());
                    var notams = new List<NotamDto>();
                    foreach (var xmlElement in aixm.EnumerateArray())
                    {
                        var xmlString = xmlElement.GetString();
                        if (string.IsNullOrWhiteSpace(xmlString))
                            continue;

                        var notam = AixmNotamParser.ParseSingle(xmlString, _logger);
                        if (notam != null)
                            notams.Add(notam);
                    }
                    return notams;
                }
            }

            // Fallback: GeoJSON FeatureCollection format
            if (root.TryGetProperty("features", out var features))
            {
                ValidateSchemaIfNeeded(features);

                var notams = new List<NotamDto>();
                foreach (var feature in features.EnumerateArray())
                {
                    var notam = JsonSerializer.Deserialize<NotamDto>(feature.GetRawText(), JsonOptions);
                    if (notam != null)
                    {
                        notams.Add(notam);
                    }
                }
                return notams;
            }

            // If it's a single feature (unlikely but handle it)
            if (root.TryGetProperty("type", out var type) && type.GetString() == "Feature")
            {
                ValidateSingleFeatureIfNeeded(root);

                var notam = JsonSerializer.Deserialize<NotamDto>(content, JsonOptions);
                return notam != null ? [notam] : [];
            }

            _logger.LogWarning("Unexpected NMS API response format");
            return [];
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse NMS API response");
            throw new InvalidOperationException("Failed to parse NMS API response", ex);
        }
    }

    private void ValidateSchemaIfNeeded(JsonElement arrayElement)
    {
        if (_schemaValidated)
            return;

        var firstFeature = arrayElement.EnumerateArray().FirstOrDefault();
        if (firstFeature.ValueKind == JsonValueKind.Object)
        {
            ValidateSingleFeatureIfNeeded(firstFeature);
        }
    }

    private void ValidateSingleFeatureIfNeeded(JsonElement feature)
    {
        if (_schemaValidated)
            return;

        _schemaValidated = true;

        var validationResult = NmsSchemaValidator.ValidateFeature(feature);
        if (!validationResult.HasDrift)
            return;

        if (validationResult.MissingProperties.Count > 0)
        {
            _logger.LogError(
                "NMS GeoJSON schema drift detected — missing required properties: {MissingProperties}",
                string.Join(", ", validationResult.MissingProperties));
        }

        if (validationResult.UnexpectedProperties.Count > 0)
        {
            _logger.LogWarning(
                "NMS GeoJSON schema drift detected — unexpected properties: {UnexpectedProperties}",
                string.Join(", ", validationResult.UnexpectedProperties));
        }
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        // Quick check without lock
        if (_accessToken != null && DateTime.UtcNow < _tokenExpiresAt.AddSeconds(-TokenRefreshBufferSeconds))
        {
            return _accessToken;
        }

        await _tokenLock.WaitAsync(ct);
        try
        {
            // Double-check after acquiring lock
            if (_accessToken != null && DateTime.UtcNow < _tokenExpiresAt.AddSeconds(-TokenRefreshBufferSeconds))
            {
                return _accessToken;
            }

            return await RefreshTokenInternalAsync(ct);
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<string> ForceRefreshTokenAsync(CancellationToken ct)
    {
        await _tokenLock.WaitAsync(ct);
        try
        {
            return await RefreshTokenInternalAsync(ct);
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<string> RefreshTokenInternalAsync(CancellationToken ct)
    {
        _logger.LogInformation("Refreshing NMS API OAuth2 token");

        if (string.IsNullOrEmpty(_settings.ClientId) || string.IsNullOrEmpty(_settings.ClientSecret))
        {
            throw new InvalidOperationException("NMS API ClientId and ClientSecret must be configured");
        }

        var client = _httpClientFactory.CreateClient("NmsApi");
        // Auth endpoint is at the root URL, not under /nmsapi
        var tokenUrl = $"{_settings.AuthBaseUrl}/v1/auth/token";

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}"));

        var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<NmsTokenResponseDto>(JsonOptions, ct);

        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException("Failed to obtain access token from NMS API");
        }

        _accessToken = tokenResponse.AccessToken;
        _tokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

        _logger.LogInformation("NMS API token refreshed, expires at {ExpiresAt}", _tokenExpiresAt);

        return _accessToken;
    }
}

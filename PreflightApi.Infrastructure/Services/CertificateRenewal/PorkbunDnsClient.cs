using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Settings;

namespace PreflightApi.Infrastructure.Services.CertificateRenewal;

public class PorkbunDnsClient : IPorkbunDnsClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PorkbunSettings _settings;
    private readonly ILogger<PorkbunDnsClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public PorkbunDnsClient(
        IHttpClientFactory httpClientFactory,
        IOptions<PorkbunSettings> settings,
        ILogger<PorkbunDnsClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task CreateTxtRecordAsync(string rootDomain, string subdomain, string content, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("Porkbun");
        var url = $"https://api.porkbun.com/api/json/v3/dns/create/{rootDomain}";

        var payload = new
        {
            apikey = _settings.ApiKey,
            secretapikey = _settings.SecretApiKey,
            type = "TXT",
            name = subdomain,
            content,
            ttl = "300"
        };

        _logger.LogInformation("Creating TXT record: {Subdomain}.{Domain}", subdomain, rootDomain);

        var response = await client.PostAsJsonAsync(url, payload, JsonOptions, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Porkbun DNS create failed: {StatusCode} {Body}", response.StatusCode, body);
            throw new HttpRequestException($"Porkbun DNS create failed ({response.StatusCode}): {body}");
        }

        _logger.LogInformation("TXT record created successfully for {Subdomain}.{Domain}", subdomain, rootDomain);
    }

    public async Task DeleteTxtRecordAsync(string rootDomain, string subdomain, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("Porkbun");
        var url = $"https://api.porkbun.com/api/json/v3/dns/deleteByNameType/{rootDomain}/TXT/{subdomain}";

        var payload = new
        {
            apikey = _settings.ApiKey,
            secretapikey = _settings.SecretApiKey
        };

        _logger.LogInformation("Deleting TXT record: {Subdomain}.{Domain}", subdomain, rootDomain);

        var response = await client.PostAsJsonAsync(url, payload, JsonOptions, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Porkbun DNS delete failed (non-critical): {StatusCode} {Body}", response.StatusCode, body);
            return;
        }

        _logger.LogInformation("TXT record deleted successfully for {Subdomain}.{Domain}", subdomain, rootDomain);
    }
}

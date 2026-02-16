using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PreflightApi.MCP.Models;

namespace PreflightApi.MCP.Services;

/// <summary>
/// HTTP client wrapper for calling PreflightApi endpoints.
/// </summary>
public class PreflightApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public PreflightApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Searches for airports by name, city, or code.
    /// </summary>
    public async Task<(List<AirportResponse>? Airports, string? Error)> SearchAirportsAsync(
        string query, int limit = 10, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"api/v1/airports?search={Uri.EscapeDataString(query)}&limit={limit}", ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await ParseErrorAsync(response, ct);
                return (null, error);
            }

            var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<AirportResponse>>(_jsonOptions, ct);
            return (result?.Data, null);
        }
        catch (Exception ex)
        {
            return (null, $"Failed to search airports: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a specific airport by ICAO code or FAA identifier.
    /// </summary>
    public async Task<(AirportResponse? Airport, string? Error)> GetAirportAsync(
        string code, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"api/v1/airports/{Uri.EscapeDataString(code)}", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return (null, $"Airport '{code}' not found");
            }

            if (!response.IsSuccessStatusCode)
            {
                var error = await ParseErrorAsync(response, ct);
                return (null, error);
            }

            var airport = await response.Content.ReadFromJsonAsync<AirportResponse>(_jsonOptions, ct);
            return (airport, null);
        }
        catch (Exception ex)
        {
            return (null, $"Failed to get airport: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets METAR for an airport.
    /// </summary>
    public async Task<(MetarResponse? Metar, string? Error)> GetMetarAsync(
        string code, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"api/v1/metars/{Uri.EscapeDataString(code)}", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return (null, $"No METAR found for '{code}'");
            }

            if (!response.IsSuccessStatusCode)
            {
                var error = await ParseErrorAsync(response, ct);
                return (null, error);
            }

            var metar = await response.Content.ReadFromJsonAsync<MetarResponse>(_jsonOptions, ct);
            return (metar, null);
        }
        catch (Exception ex)
        {
            return (null, $"Failed to get METAR: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets TAF for an airport.
    /// </summary>
    public async Task<(TafResponse? Taf, string? Error)> GetTafAsync(
        string code, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"api/v1/tafs/{Uri.EscapeDataString(code)}", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return (null, $"No TAF found for '{code}'");
            }

            if (!response.IsSuccessStatusCode)
            {
                var error = await ParseErrorAsync(response, ct);
                return (null, error);
            }

            var taf = await response.Content.ReadFromJsonAsync<TafResponse>(_jsonOptions, ct);
            return (taf, null);
        }
        catch (Exception ex)
        {
            return (null, $"Failed to get TAF: {ex.Message}");
        }
    }

    /// <summary>
    /// Calculates a navigation log.
    /// </summary>
    public async Task<(NavlogResponse? Navlog, string? Error)> CalculateNavlogAsync(
        NavlogRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/navlog/calculate", request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await ParseErrorAsync(response, ct);
                return (null, error);
            }

            var navlog = await response.Content.ReadFromJsonAsync<NavlogResponse>(_jsonOptions, ct);
            return (navlog, null);
        }
        catch (Exception ex)
        {
            return (null, $"Failed to calculate navlog: {ex.Message}");
        }
    }

    private async Task<string> ParseErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync(ct);
            var error = JsonSerializer.Deserialize<ApiErrorResponse>(content, _jsonOptions);
            return error?.Detail ?? error?.Title ?? $"API error: {response.StatusCode}";
        }
        catch
        {
            return $"API error: {response.StatusCode}";
        }
    }
}

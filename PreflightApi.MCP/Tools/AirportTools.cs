using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using PreflightApi.MCP.Models;
using PreflightApi.MCP.Services;

namespace PreflightApi.MCP.Tools;

/// <summary>
/// MCP tools for airport search and lookup operations.
/// </summary>
[McpServerToolType]
public class AirportTools
{
    private readonly PreflightApiClient _apiClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public AirportTools(PreflightApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Searches for airports by name, city, ICAO code, or FAA identifier.
    /// Use this when the user provides a location name or partial code.
    /// Returns uncertainty metadata when multiple airports match the search.
    /// </summary>
    /// <param name="query">Search query (airport name, city, ICAO code, or FAA identifier)</param>
    /// <param name="limit">Maximum number of results to return (default: 10, max: 50)</param>
    [McpServerTool]
    [Description("Search for airports by name, city, ICAO code, or FAA identifier. Use this when the user provides a location name like 'Nashville' or 'Dallas' rather than a specific airport code.")]
    public async Task<string> SearchAirports(
        [Description("Search query - can be airport name, city name, ICAO code (e.g., KBNA), or FAA identifier (e.g., BNA)")] string query,
        [Description("Maximum results to return (1-50, default 10)")] int limit = 10)
    {
        limit = Math.Clamp(limit, 1, 50);

        var (airports, error) = await _apiClient.SearchAirportsAsync(query, limit);

        if (error != null)
        {
            var errorResponse = McpToolResponse<object>.Fail(error);
            return JsonSerializer.Serialize(errorResponse, JsonOptions);
        }

        if (airports == null || airports.Count == 0)
        {
            var notFoundResponse = McpToolResponse<object>.Fail(
                $"No airports found matching '{query}'",
                [
                    new UncertaintyItem
                    {
                        Type = UncertaintyType.NotFound,
                        Message = $"No airports found matching '{query}'",
                        SuggestedPrompt = "Could you provide more details about the airport you're looking for? You can specify the city name, state, or try a different spelling.",
                        Severity = UncertaintySeverity.Warning
                    }
                ]);
            return JsonSerializer.Serialize(notFoundResponse, JsonOptions);
        }

        var uncertainties = new List<UncertaintyItem>();

        // Check for ambiguity - multiple airports matching
        if (airports.Count > 1)
        {
            var airportList = string.Join(", ", airports.Take(5).Select(a =>
                $"{a.IcaoId ?? a.ArptId} ({a.ArptName}, {a.City}, {a.StateCode})"));

            uncertainties.Add(new UncertaintyItem
            {
                Type = UncertaintyType.AmbiguousInput,
                Message = $"Found {airports.Count} airports matching '{query}': {airportList}" +
                          (airports.Count > 5 ? $" and {airports.Count - 5} more" : ""),
                SuggestedPrompt = "Which airport did you mean? Please specify the ICAO code (like KBNA) or FAA identifier (like BNA), or clarify with the city and state.",
                Severity = UncertaintySeverity.Warning
            });
        }

        // Transform to simplified format
        var simplified = airports.Select(a => new
        {
            Code = a.IcaoId ?? a.ArptId,
            a.ArptName,
            a.City,
            a.StateCode,
            Latitude = a.LatDecimal,
            Longitude = a.LongDecimal,
            ElevationFt = a.Elev,
            HasTower = a.TwrTypeCode == "ATCT",
            a.FuelTypes
        }).ToList();

        var summary = airports.Count == 1
            ? $"Found {airports[0].ArptName} ({airports[0].IcaoId ?? airports[0].ArptId}) in {airports[0].City}, {airports[0].StateCode}"
            : $"Found {airports.Count} airports matching '{query}'";

        var response = McpToolResponse<object>.Ok(simplified, summary, uncertainties);
        return JsonSerializer.Serialize(response, JsonOptions);
    }

    /// <summary>
    /// Gets detailed information for a specific airport by its ICAO code or FAA identifier.
    /// Use this when you have the exact airport code.
    /// </summary>
    /// <param name="code">ICAO code (e.g., KBNA) or FAA identifier (e.g., BNA)</param>
    [McpServerTool]
    [Description("Get detailed airport information by ICAO code or FAA identifier. Use this when you have the specific airport code like KBNA or BNA.")]
    public async Task<string> GetAirport(
        [Description("Airport ICAO code (e.g., KBNA, KATL) or FAA identifier (e.g., BNA, ATL)")] string code)
    {
        var (airport, error) = await _apiClient.GetAirportAsync(code);

        if (error != null)
        {
            var uncertainties = new List<UncertaintyItem>();

            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                uncertainties.Add(new UncertaintyItem
                {
                    Type = UncertaintyType.NotFound,
                    Message = $"Airport '{code}' not found in the database",
                    SuggestedPrompt = $"I couldn't find airport '{code}'. Would you like me to search for airports by name or city instead?",
                    Severity = UncertaintySeverity.Warning
                });
            }

            var errorResponse = McpToolResponse<object>.Fail(error, uncertainties);
            return JsonSerializer.Serialize(errorResponse, JsonOptions);
        }

        if (airport == null)
        {
            var notFoundResponse = McpToolResponse<object>.Fail($"Airport '{code}' not found");
            return JsonSerializer.Serialize(notFoundResponse, JsonOptions);
        }

        var simplified = new
        {
            Code = airport.IcaoId ?? airport.ArptId,
            IcaoCode = airport.IcaoId,
            FaaId = airport.ArptId,
            Name = airport.ArptName,
            airport.City,
            State = airport.StateCode,
            Latitude = airport.LatDecimal,
            Longitude = airport.LongDecimal,
            ElevationFt = airport.Elev,
            TrafficPatternAltitude = airport.Tpa,
            HasTower = airport.TwrTypeCode == "ATCT",
            airport.FuelTypes,
            HasNotams = airport.NotamAvailable
        };

        var summary = $"{airport.ArptName} ({airport.IcaoId ?? airport.ArptId}) - {airport.City}, {airport.StateCode}, Elevation: {airport.Elev}ft";

        var response = McpToolResponse<object>.Ok(simplified, summary);
        return JsonSerializer.Serialize(response, JsonOptions);
    }
}

using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using PreflightApi.MCP.Models;
using PreflightApi.MCP.Services;

namespace PreflightApi.MCP.Tools;

/// <summary>
/// MCP tools for aviation safety information (airspace, NOTAMs, briefings).
/// </summary>
[McpServerToolType]
public class SafetyTools
{
    private readonly PreflightApiClient _apiClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public SafetyTools(PreflightApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Gets a combined route briefing including weather, airport information, and safety notes.
    /// This is a comprehensive briefing for pre-flight planning.
    /// </summary>
    [McpServerTool]
    [Description("Get a comprehensive route briefing including current weather at departure and destination, airport information, and safety considerations. Use this for a complete pre-flight overview of a planned route.")]
    public async Task<string> GetRouteBriefing(
        [Description("Departure airport code (ICAO or FAA identifier)")] string departureAirport,
        [Description("Destination airport code (ICAO or FAA identifier)")] string destinationAirport,
        [Description("Intermediate waypoint codes, comma-separated (optional)")] string? intermediateWaypoints)
    {
        var allAirports = new List<string> { departureAirport, destinationAirport };
        if (!string.IsNullOrWhiteSpace(intermediateWaypoints))
        {
            var intermediate = intermediateWaypoints.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            allAirports.AddRange(intermediate);
        }

        var uncertainties = new List<UncertaintyItem>();
        var briefingSections = new Dictionary<string, object>();
        var overallConcerns = new List<string>();

        // Gather airport and weather data
        var airportBriefings = new List<object>();

        foreach (var code in allAirports)
        {
            var airportBriefing = new Dictionary<string, object?> { ["airportCode"] = code };

            // Get airport info
            var (airport, airportError) = await _apiClient.GetAirportAsync(code);
            if (airport != null)
            {
                airportBriefing["airportInfo"] = new
                {
                    Name = airport.ArptName,
                    airport.City,
                    State = airport.StateCode,
                    ElevationFt = airport.Elev,
                    HasTower = airport.TwrTypeCode == "ATCT",
                    airport.FuelTypes
                };
            }
            else
            {
                uncertainties.Add(new UncertaintyItem
                {
                    Type = UncertaintyType.NotFound,
                    Message = $"Could not retrieve airport information for {code}",
                    Severity = UncertaintySeverity.Warning
                });
            }

            // Get METAR
            var (metar, metarError) = await _apiClient.GetMetarAsync(code);
            if (metar != null)
            {
                airportBriefing["currentWeather"] = new
                {
                    metar.FlightCategory,
                    metar.ObservationTime,
                    Wind = FormatWind(metar.WindDirDegrees, metar.WindSpeedKt, metar.WindGustKt),
                    Visibility = metar.VisibilityStatuteMi,
                    Ceiling = GetCeiling(metar.SkyCondition),
                    Weather = metar.WxString,
                    Temperature = metar.TempC,
                    Altimeter = metar.AltimInHg
                };

                // Check for weather concerns
                if (metar.FlightCategory == "IFR" || metar.FlightCategory == "LIFR")
                {
                    overallConcerns.Add($"{code}: {metar.FlightCategory} conditions");
                    uncertainties.Add(new UncertaintyItem
                    {
                        Type = UncertaintyType.WeatherConcern,
                        Message = $"{code} is reporting {metar.FlightCategory} conditions",
                        Severity = UncertaintySeverity.Critical
                    });
                }
                else if (metar.FlightCategory == "MVFR")
                {
                    overallConcerns.Add($"{code}: Marginal VFR");
                    uncertainties.Add(new UncertaintyItem
                    {
                        Type = UncertaintyType.WeatherConcern,
                        Message = $"{code} is reporting Marginal VFR conditions",
                        Severity = UncertaintySeverity.Warning
                    });
                }

                if (!string.IsNullOrEmpty(metar.WxString))
                {
                    var wx = metar.WxString.ToUpperInvariant();
                    if (wx.Contains("TS"))
                    {
                        overallConcerns.Add($"{code}: Thunderstorms");
                        uncertainties.Add(new UncertaintyItem
                        {
                            Type = UncertaintyType.WeatherConcern,
                            Message = $"Thunderstorms reported at {code}",
                            Severity = UncertaintySeverity.Critical
                        });
                    }
                }

                if (metar.WindGustKt.HasValue && metar.WindGustKt > 25)
                {
                    overallConcerns.Add($"{code}: Gusty winds {metar.WindGustKt}kt");
                }
            }
            else
            {
                airportBriefing["currentWeather"] = null;
                uncertainties.Add(new UncertaintyItem
                {
                    Type = UncertaintyType.NotFound,
                    Message = $"No current weather available for {code}",
                    SuggestedPrompt = $"Weather data is not available for {code}. This airport may not have weather reporting. Would you like to check a nearby airport?",
                    Severity = UncertaintySeverity.Warning
                });
            }

            // Get TAF for departure and destination
            if (code == departureAirport || code == destinationAirport)
            {
                var (taf, tafError) = await _apiClient.GetTafAsync(code);
                if (taf != null)
                {
                    airportBriefing["forecast"] = new
                    {
                        ValidFrom = taf.ValidTimeFrom,
                        ValidTo = taf.ValidTimeTo,
                        taf.IssueTime,
                        Periods = taf.Forecast?.Take(4).Select(p => new
                        {
                            From = p.FcstTimeFrom,
                            To = p.FcstTimeTo,
                            Type = p.ChangeIndicator ?? "BASE",
                            Wind = FormatWind(p.WindDirDegrees, p.WindSpeedKt, p.WindGustKt),
                            Visibility = p.VisibilityStatuteMi,
                            Weather = p.WxString
                        }).ToList()
                    };

                    // Check forecast for adverse weather
                    if (taf.Forecast != null)
                    {
                        foreach (var period in taf.Forecast)
                        {
                            if (!string.IsNullOrEmpty(period.WxString) &&
                                period.WxString.ToUpperInvariant().Contains("TS"))
                            {
                                uncertainties.Add(new UncertaintyItem
                                {
                                    Type = UncertaintyType.WeatherConcern,
                                    Message = $"Thunderstorms forecast at {code} from {period.FcstTimeFrom} to {period.FcstTimeTo}",
                                    Severity = UncertaintySeverity.Critical
                                });
                            }
                        }
                    }
                }
            }

            airportBriefings.Add(airportBriefing);
        }

        briefingSections["airports"] = airportBriefings;

        // Determine overall route status
        var hasNoGo = uncertainties.Any(u => u.Severity == UncertaintySeverity.Critical);
        var hasCaution = uncertainties.Any(u => u.Severity == UncertaintySeverity.Warning);

        string routeStatus;
        if (hasNoGo)
        {
            routeStatus = "NO-GO";
        }
        else if (hasCaution)
        {
            routeStatus = "CAUTION";
        }
        else
        {
            routeStatus = "GO";
        }

        briefingSections["routeStatus"] = routeStatus;
        briefingSections["concerns"] = overallConcerns;
        briefingSections["briefingTime"] = DateTime.UtcNow.ToString("o");

        var summary = $"Route briefing {departureAirport} to {destinationAirport}: {routeStatus}";
        if (overallConcerns.Count > 0)
        {
            summary += $". Concerns: {string.Join("; ", overallConcerns)}";
        }

        var response = McpToolResponse<object>.Ok(briefingSections, summary, uncertainties);
        return JsonSerializer.Serialize(response, JsonOptions);
    }

    private static int? GetCeiling(List<SkyConditionResponse>? skyConditions)
    {
        if (skyConditions == null) return null;

        var ceiling = skyConditions
            .Where(s => s.SkyCover is "BKN" or "OVC" && s.CloudBaseFtAgl.HasValue)
            .OrderBy(s => s.CloudBaseFtAgl)
            .FirstOrDefault();

        return ceiling?.CloudBaseFtAgl;
    }

    private static string FormatWind(string? direction, int? speed, int? gust)
    {
        if (!speed.HasValue || speed == 0) return "Calm";

        var wind = $"{direction ?? "VRB"}@{speed}kt";
        if (gust.HasValue && gust > speed)
        {
            wind += $" G{gust}";
        }
        return wind;
    }
}

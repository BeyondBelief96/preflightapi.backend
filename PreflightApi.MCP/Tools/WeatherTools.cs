using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using PreflightApi.MCP.Models;
using PreflightApi.MCP.Services;

namespace PreflightApi.MCP.Tools;

/// <summary>
/// MCP tools for aviation weather data retrieval and assessment.
/// </summary>
[McpServerToolType]
public class WeatherTools
{
    private readonly PreflightApiClient _apiClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public WeatherTools(PreflightApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Gets the current METAR (weather observation) for an airport.
    /// </summary>
    [McpServerTool]
    [Description("Get the current METAR weather observation for an airport. Returns decoded weather data including wind, visibility, clouds, temperature, and flight category (VFR/MVFR/IFR/LIFR).")]
    public async Task<string> GetMetar(
        [Description("Airport ICAO code (e.g., KBNA) or FAA identifier (e.g., BNA)")] string airportCode)
    {
        var (metar, error) = await _apiClient.GetMetarAsync(airportCode);

        if (error != null)
        {
            var errorUncertainties = new List<UncertaintyItem>();

            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                errorUncertainties.Add(new UncertaintyItem
                {
                    Type = UncertaintyType.NotFound,
                    Message = $"No METAR available for '{airportCode}'",
                    SuggestedPrompt = "This airport may not have weather reporting. Would you like me to check a nearby airport with weather services?",
                    Severity = UncertaintySeverity.Warning
                });
            }

            return JsonSerializer.Serialize(McpToolResponse<object>.Fail(error, errorUncertainties), JsonOptions);
        }

        if (metar == null)
        {
            return JsonSerializer.Serialize(McpToolResponse<object>.Fail($"No METAR data for '{airportCode}'"), JsonOptions);
        }

        var uncertainties = new List<UncertaintyItem>();

        // Check for weather concerns
        if (metar.FlightCategory == "IFR" || metar.FlightCategory == "LIFR")
        {
            uncertainties.Add(new UncertaintyItem
            {
                Type = UncertaintyType.WeatherConcern,
                Message = $"Current conditions are {metar.FlightCategory} - instrument flight rules apply",
                Severity = metar.FlightCategory == "LIFR" ? UncertaintySeverity.Critical : UncertaintySeverity.Warning
            });
        }

        // Check for significant weather
        if (!string.IsNullOrEmpty(metar.WxString))
        {
            var wxLower = metar.WxString.ToLowerInvariant();
            if (wxLower.Contains("ts") || wxLower.Contains("fc") || wxLower.Contains("fz"))
            {
                uncertainties.Add(new UncertaintyItem
                {
                    Type = UncertaintyType.WeatherConcern,
                    Message = $"Significant weather reported: {metar.WxString}",
                    Severity = UncertaintySeverity.Critical
                });
            }
        }

        // Check for gusty winds
        if (metar.WindGustKt.HasValue && metar.WindGustKt > 25)
        {
            uncertainties.Add(new UncertaintyItem
            {
                Type = UncertaintyType.WeatherConcern,
                Message = $"Strong wind gusts reported: {metar.WindGustKt} knots",
                Severity = UncertaintySeverity.Warning
            });
        }

        // Check observation age
        if (DateTime.TryParse(metar.ObservationTime, out var obsTime))
        {
            var age = DateTime.UtcNow - obsTime;
            if (age.TotalMinutes > 90)
            {
                uncertainties.Add(new UncertaintyItem
                {
                    Type = UncertaintyType.StaleData,
                    Message = $"METAR is {(int)age.TotalMinutes} minutes old - conditions may have changed",
                    Severity = UncertaintySeverity.Info
                });
            }
        }

        var ceiling = GetCeiling(metar.SkyCondition);

        var simplified = new
        {
            metar.StationId,
            metar.FlightCategory,
            metar.ObservationTime,
            Wind = FormatWind(metar.WindDirDegrees, metar.WindSpeedKt, metar.WindGustKt),
            Visibility = metar.VisibilityStatuteMi,
            Ceiling = ceiling,
            Clouds = metar.SkyCondition?.Select(c => $"{c.SkyCover} {c.CloudBaseFtAgl}").ToList(),
            Temperature = metar.TempC,
            Dewpoint = metar.DewpointC,
            Altimeter = metar.AltimInHg,
            Weather = metar.WxString,
            RawMetar = metar.RawText
        };

        var summary = $"{metar.StationId}: {metar.FlightCategory}, " +
                      $"Wind {FormatWind(metar.WindDirDegrees, metar.WindSpeedKt, metar.WindGustKt)}, " +
                      $"Vis {metar.VisibilityStatuteMi}SM" +
                      (ceiling.HasValue ? $", Ceiling {ceiling}ft" : "");

        return JsonSerializer.Serialize(McpToolResponse<object>.Ok(simplified, summary, uncertainties), JsonOptions);
    }

    /// <summary>
    /// Gets the terminal forecast (TAF) for an airport.
    /// </summary>
    [McpServerTool]
    [Description("Get the TAF terminal forecast for an airport. TAFs provide weather forecasts for airports, typically covering 24-30 hours with specific forecast periods.")]
    public async Task<string> GetTaf(
        [Description("Airport ICAO code (e.g., KBNA) or FAA identifier (e.g., BNA)")] string airportCode)
    {
        var (taf, error) = await _apiClient.GetTafAsync(airportCode);

        if (error != null)
        {
            var errorUncertainties = new List<UncertaintyItem>();

            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                errorUncertainties.Add(new UncertaintyItem
                {
                    Type = UncertaintyType.NotFound,
                    Message = $"No TAF available for '{airportCode}'",
                    SuggestedPrompt = "This airport may not have TAF services. Would you like me to check nearby airports for forecast information?",
                    Severity = UncertaintySeverity.Warning
                });
            }

            return JsonSerializer.Serialize(McpToolResponse<object>.Fail(error, errorUncertainties), JsonOptions);
        }

        if (taf == null)
        {
            return JsonSerializer.Serialize(McpToolResponse<object>.Fail($"No TAF data for '{airportCode}'"), JsonOptions);
        }

        var uncertainties = new List<UncertaintyItem>();

        // Check for adverse weather in forecast periods
        if (taf.Forecast != null)
        {
            foreach (var period in taf.Forecast)
            {
                if (!string.IsNullOrEmpty(period.WxString))
                {
                    var wxLower = period.WxString.ToLowerInvariant();
                    if (wxLower.Contains("ts"))
                    {
                        uncertainties.Add(new UncertaintyItem
                        {
                            Type = UncertaintyType.WeatherConcern,
                            Message = $"Thunderstorms forecast: {period.WxString} from {period.FcstTimeFrom} to {period.FcstTimeTo}",
                            Severity = UncertaintySeverity.Critical
                        });
                    }
                }

                // Check for low visibility/ceiling
                if (float.TryParse(period.VisibilityStatuteMi?.Replace("+", ""), out var vis) && vis < 3)
                {
                    uncertainties.Add(new UncertaintyItem
                    {
                        Type = UncertaintyType.WeatherConcern,
                        Message = $"Low visibility forecast: {period.VisibilityStatuteMi}SM from {period.FcstTimeFrom}",
                        Severity = vis < 1 ? UncertaintySeverity.Critical : UncertaintySeverity.Warning
                    });
                }
            }
        }

        var simplified = new
        {
            taf.StationId,
            taf.IssueTime,
            ValidFrom = taf.ValidTimeFrom,
            ValidTo = taf.ValidTimeTo,
            Periods = taf.Forecast?.Select(p => new
            {
                From = p.FcstTimeFrom,
                To = p.FcstTimeTo,
                Type = p.ChangeIndicator ?? "BASE",
                Wind = FormatWind(p.WindDirDegrees, p.WindSpeedKt, p.WindGustKt),
                Visibility = p.VisibilityStatuteMi,
                Weather = p.WxString,
                Clouds = p.SkyConditions?.Select(c => $"{c.SkyCover} {c.CloudBaseFtAgl}").ToList()
            }).ToList(),
            RawTaf = taf.RawText
        };

        var summary = $"{taf.StationId} TAF valid {taf.ValidTimeFrom} to {taf.ValidTimeTo}";

        return JsonSerializer.Serialize(McpToolResponse<object>.Ok(simplified, summary, uncertainties), JsonOptions);
    }

    /// <summary>
    /// Assesses VFR weather conditions for a route or set of airports.
    /// Returns GO/CAUTION/NO-GO assessment with specific concerns.
    /// </summary>
    [McpServerTool]
    [Description("Assess VFR weather conditions for one or more airports. Returns a GO/CAUTION/NO-GO assessment based on current conditions at each airport. Use this to evaluate if a VFR flight is advisable.")]
    public async Task<string> AssessVfrWeather(
        [Description("Comma-separated list of airport codes to check (e.g., 'KBNA,KATL' or 'BNA,ATL')")] string airportCodes)
    {
        var codes = airportCodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (codes.Length == 0)
        {
            return JsonSerializer.Serialize(McpToolResponse<object>.Fail("No airport codes provided"), JsonOptions);
        }

        var assessments = new List<VfrWeatherAssessment>();
        var allUncertainties = new List<UncertaintyItem>();
        var overallAssessment = "GO";

        foreach (var code in codes)
        {
            var (metar, error) = await _apiClient.GetMetarAsync(code);

            if (error != null || metar == null)
            {
                assessments.Add(new VfrWeatherAssessment
                {
                    AirportCode = code,
                    FlightCategory = "UNKNOWN",
                    Assessment = "UNKNOWN",
                    Concerns = [$"No weather data available: {error ?? "Unknown error"}"]
                });

                allUncertainties.Add(new UncertaintyItem
                {
                    Type = UncertaintyType.NotFound,
                    Message = $"No weather data for {code}",
                    SuggestedPrompt = $"I couldn't get weather for {code}. Would you like to use a nearby airport for weather information?",
                    Severity = UncertaintySeverity.Warning
                });
                continue;
            }

            var concerns = new List<string>();
            var assessment = "GO";

            // Assess flight category
            switch (metar.FlightCategory)
            {
                case "LIFR":
                    assessment = "NO-GO";
                    concerns.Add("Low IFR conditions - VFR flight not possible");
                    break;
                case "IFR":
                    assessment = "NO-GO";
                    concerns.Add("IFR conditions - VFR flight not possible");
                    break;
                case "MVFR":
                    assessment = "CAUTION";
                    concerns.Add("Marginal VFR - reduced visibility or low ceiling");
                    break;
            }

            // Check visibility
            if (float.TryParse(metar.VisibilityStatuteMi?.Replace("+", ""), out var vis))
            {
                if (vis < 3)
                {
                    assessment = "NO-GO";
                    concerns.Add($"Visibility {vis}SM below VFR minimums");
                }
                else if (vis < 5)
                {
                    if (assessment != "NO-GO") assessment = "CAUTION";
                    concerns.Add($"Reduced visibility {vis}SM");
                }
            }

            // Check ceiling
            var ceiling = GetCeiling(metar.SkyCondition);
            if (ceiling.HasValue)
            {
                if (ceiling < 1000)
                {
                    assessment = "NO-GO";
                    concerns.Add($"Ceiling {ceiling}ft below VFR minimums");
                }
                else if (ceiling < 3000)
                {
                    if (assessment != "NO-GO") assessment = "CAUTION";
                    concerns.Add($"Low ceiling {ceiling}ft");
                }
            }

            // Check winds
            if (metar.WindGustKt.HasValue && metar.WindGustKt > 25)
            {
                if (assessment != "NO-GO") assessment = "CAUTION";
                concerns.Add($"Strong gusts to {metar.WindGustKt}kt");
            }

            if (metar.WindSpeedKt.HasValue && metar.WindSpeedKt > 20)
            {
                if (assessment != "NO-GO") assessment = "CAUTION";
                concerns.Add($"Strong winds {metar.WindSpeedKt}kt");
            }

            // Check weather phenomena
            if (!string.IsNullOrEmpty(metar.WxString))
            {
                var wx = metar.WxString.ToUpperInvariant();
                if (wx.Contains("TS"))
                {
                    assessment = "NO-GO";
                    concerns.Add("Thunderstorms reported");
                }
                else if (wx.Contains("FZ"))
                {
                    assessment = "NO-GO";
                    concerns.Add("Freezing precipitation");
                }
                else if (wx.Contains("FG") && !wx.Contains("BR"))
                {
                    if (assessment != "NO-GO") assessment = "CAUTION";
                    concerns.Add("Fog reported");
                }
            }

            assessments.Add(new VfrWeatherAssessment
            {
                AirportCode = code,
                FlightCategory = metar.FlightCategory ?? "UNKNOWN",
                Assessment = assessment,
                Visibility = metar.VisibilityStatuteMi,
                CeilingFeet = ceiling,
                Wind = FormatWind(metar.WindDirDegrees, metar.WindSpeedKt, metar.WindGustKt),
                Weather = metar.WxString,
                ObservationTime = metar.ObservationTime,
                RawMetar = metar.RawText,
                Concerns = concerns
            });

            // Update overall assessment
            if (assessment == "NO-GO")
            {
                overallAssessment = "NO-GO";
            }
            else if (assessment == "CAUTION" && overallAssessment != "NO-GO")
            {
                overallAssessment = "CAUTION";
            }
        }

        // Add uncertainties for critical conditions
        foreach (var a in assessments.Where(a => a.Assessment == "NO-GO"))
        {
            allUncertainties.Add(new UncertaintyItem
            {
                Type = UncertaintyType.WeatherConcern,
                Message = $"{a.AirportCode}: NO-GO - {string.Join(", ", a.Concerns)}",
                Severity = UncertaintySeverity.Critical
            });
        }

        foreach (var a in assessments.Where(a => a.Assessment == "CAUTION"))
        {
            allUncertainties.Add(new UncertaintyItem
            {
                Type = UncertaintyType.WeatherConcern,
                Message = $"{a.AirportCode}: CAUTION - {string.Join(", ", a.Concerns)}",
                Severity = UncertaintySeverity.Warning
            });
        }

        var result = new RouteWeatherAssessment
        {
            OverallAssessment = overallAssessment,
            Airports = assessments,
            Summary = $"Route assessment: {overallAssessment}. " +
                      string.Join(". ", assessments.Select(a => $"{a.AirportCode}: {a.FlightCategory} ({a.Assessment})"))
        };

        return JsonSerializer.Serialize(McpToolResponse<RouteWeatherAssessment>.Ok(result, result.Summary, allUncertainties), JsonOptions);
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

using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using PreflightApi.MCP.Models;
using PreflightApi.MCP.Services;

namespace PreflightApi.MCP.Tools;

/// <summary>
/// MCP tools for flight planning validation and navigation log calculation.
/// </summary>
[McpServerToolType]
public class FlightPlanningTools
{
    private readonly PreflightApiClient _apiClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public FlightPlanningTools(PreflightApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Validates that all required inputs for a flight plan are present.
    /// Returns a list of missing fields with suggested prompts to gather them from the user.
    /// </summary>
    [McpServerTool]
    [Description("Validate flight plan inputs and identify missing required data. Use this BEFORE calculating a navigation log to ensure all necessary aircraft performance data and route information is available. Returns specific questions to ask the user for any missing information.")]
    public Task<string> ValidateFlightPlanInputs(
        [Description("Departure airport code (ICAO or FAA identifier)")] string? departureAirport,
        [Description("Destination airport code (ICAO or FAA identifier)")] string? destinationAirport,
        [Description("Intermediate waypoint codes, comma-separated (optional)")] string? intermediateWaypoints,
        [Description("Departure time in UTC (ISO 8601 format, e.g., '2024-01-15T14:00:00Z')")] string? departureTimeUtc,
        [Description("Cruising altitude in feet MSL (e.g., 5500)")] int? cruisingAltitude,
        [Description("Cruise true airspeed in knots")] int? cruiseTas,
        [Description("Climb true airspeed in knots")] int? climbTas,
        [Description("Descent true airspeed in knots")] int? descentTas,
        [Description("Climb rate in feet per minute")] int? climbRate,
        [Description("Descent rate in feet per minute")] int? descentRate,
        [Description("Cruise fuel burn in gallons per hour")] double? cruiseFuelBurn,
        [Description("Climb fuel burn in gallons per hour")] double? climbFuelBurn,
        [Description("Descent fuel burn in gallons per hour")] double? descentFuelBurn,
        [Description("Start/taxi/takeoff fuel in gallons")] double? sttFuel,
        [Description("Total fuel on board at departure in gallons")] double? fuelOnBoard)
    {
        var uncertainties = new List<UncertaintyItem>();
        var missingFields = new List<string>();

        // Required route information
        if (string.IsNullOrWhiteSpace(departureAirport))
        {
            missingFields.Add("departureAirport");
            uncertainties.Add(new UncertaintyItem
            {
                Type = UncertaintyType.MissingRequiredField,
                Message = "Departure airport is required",
                SuggestedPrompt = "What airport will you be departing from? Please provide the airport code (like KBNA or BNA) or name.",
                Severity = UncertaintySeverity.Critical
            });
        }

        if (string.IsNullOrWhiteSpace(destinationAirport))
        {
            missingFields.Add("destinationAirport");
            uncertainties.Add(new UncertaintyItem
            {
                Type = UncertaintyType.MissingRequiredField,
                Message = "Destination airport is required",
                SuggestedPrompt = "What is your destination airport? Please provide the airport code or name.",
                Severity = UncertaintySeverity.Critical
            });
        }

        if (string.IsNullOrWhiteSpace(departureTimeUtc))
        {
            missingFields.Add("departureTimeUtc");
            uncertainties.Add(new UncertaintyItem
            {
                Type = UncertaintyType.MissingRequiredField,
                Message = "Departure time is required for wind calculations",
                SuggestedPrompt = "When do you plan to depart? Please provide the date and time (local time is fine, I'll convert to UTC).",
                Severity = UncertaintySeverity.Critical
            });
        }

        if (!cruisingAltitude.HasValue)
        {
            missingFields.Add("cruisingAltitude");
            uncertainties.Add(new UncertaintyItem
            {
                Type = UncertaintyType.MissingRequiredField,
                Message = "Cruising altitude is required",
                SuggestedPrompt = "What altitude do you plan to cruise at? (For VFR, remember odd thousands +500 eastbound, even thousands +500 westbound)",
                Severity = UncertaintySeverity.Critical
            });
        }

        // Required performance data - airspeeds
        if (!cruiseTas.HasValue)
        {
            missingFields.Add("cruiseTas");
            uncertainties.Add(new UncertaintyItem
            {
                Type = UncertaintyType.MissingRequiredField,
                Message = "Cruise true airspeed is required",
                SuggestedPrompt = "What is your aircraft's cruise true airspeed in knots?",
                Severity = UncertaintySeverity.Critical
            });
        }

        if (!climbTas.HasValue)
        {
            missingFields.Add("climbTas");
            uncertainties.Add(new UncertaintyItem
            {
                Type = UncertaintyType.MissingRequiredField,
                Message = "Climb true airspeed is required",
                SuggestedPrompt = "What is your climb true airspeed in knots?",
                Severity = UncertaintySeverity.Critical
            });
        }

        if (!descentTas.HasValue)
        {
            missingFields.Add("descentTas");
            uncertainties.Add(new UncertaintyItem
            {
                Type = UncertaintyType.MissingRequiredField,
                Message = "Descent true airspeed is required",
                SuggestedPrompt = "What is your descent true airspeed in knots?",
                Severity = UncertaintySeverity.Critical
            });
        }

        // Required performance data - rates
        if (!climbRate.HasValue)
        {
            missingFields.Add("climbRate");
            uncertainties.Add(new UncertaintyItem
            {
                Type = UncertaintyType.MissingRequiredField,
                Message = "Climb rate is required",
                SuggestedPrompt = "What is your aircraft's rate of climb in feet per minute (FPM)?",
                Severity = UncertaintySeverity.Critical
            });
        }

        if (!descentRate.HasValue)
        {
            missingFields.Add("descentRate");
            uncertainties.Add(new UncertaintyItem
            {
                Type = UncertaintyType.MissingRequiredField,
                Message = "Descent rate is required",
                SuggestedPrompt = "What is your planned descent rate in feet per minute (FPM)?",
                Severity = UncertaintySeverity.Critical
            });
        }

        // Required performance data - fuel
        if (!cruiseFuelBurn.HasValue)
        {
            missingFields.Add("cruiseFuelBurn");
            uncertainties.Add(new UncertaintyItem
            {
                Type = UncertaintyType.MissingRequiredField,
                Message = "Cruise fuel burn rate is required",
                SuggestedPrompt = "What is your cruise fuel consumption in gallons per hour?",
                Severity = UncertaintySeverity.Critical
            });
        }

        if (!climbFuelBurn.HasValue)
        {
            missingFields.Add("climbFuelBurn");
            uncertainties.Add(new UncertaintyItem
            {
                Type = UncertaintyType.MissingRequiredField,
                Message = "Climb fuel burn rate is required",
                SuggestedPrompt = "What is your fuel consumption during climb in gallons per hour?",
                Severity = UncertaintySeverity.Critical
            });
        }

        if (!descentFuelBurn.HasValue)
        {
            missingFields.Add("descentFuelBurn");
            uncertainties.Add(new UncertaintyItem
            {
                Type = UncertaintyType.MissingRequiredField,
                Message = "Descent fuel burn rate is required",
                SuggestedPrompt = "What is your fuel consumption during descent in gallons per hour?",
                Severity = UncertaintySeverity.Critical
            });
        }

        if (!sttFuel.HasValue)
        {
            missingFields.Add("sttFuel");
            uncertainties.Add(new UncertaintyItem
            {
                Type = UncertaintyType.MissingRequiredField,
                Message = "Start/taxi/takeoff fuel is required",
                SuggestedPrompt = "How much fuel do you typically use for start, taxi, and takeoff (in gallons)?",
                Severity = UncertaintySeverity.Critical
            });
        }

        if (!fuelOnBoard.HasValue)
        {
            missingFields.Add("fuelOnBoard");
            uncertainties.Add(new UncertaintyItem
            {
                Type = UncertaintyType.MissingRequiredField,
                Message = "Fuel on board is required",
                SuggestedPrompt = "How much total usable fuel will you have on board at departure (in gallons)?",
                Severity = UncertaintySeverity.Critical
            });
        }

        var isValid = missingFields.Count == 0;

        var result = new
        {
            IsValid = isValid,
            MissingFields = missingFields,
            ProvidedData = new
            {
                DepartureAirport = departureAirport,
                DestinationAirport = destinationAirport,
                IntermediateWaypoints = intermediateWaypoints,
                DepartureTimeUtc = departureTimeUtc,
                CruisingAltitude = cruisingAltitude,
                CruiseTas = cruiseTas,
                ClimbTas = climbTas,
                DescentTas = descentTas,
                ClimbRate = climbRate,
                DescentRate = descentRate,
                CruiseFuelBurn = cruiseFuelBurn,
                ClimbFuelBurn = climbFuelBurn,
                DescentFuelBurn = descentFuelBurn,
                SttFuel = sttFuel,
                FuelOnBoard = fuelOnBoard
            }
        };

        var summary = isValid
            ? "All required flight plan inputs are present"
            : $"Missing {missingFields.Count} required field(s): {string.Join(", ", missingFields)}";

        var response = McpToolResponse<object>.Ok(result, summary, uncertainties);
        return Task.FromResult(JsonSerializer.Serialize(response, JsonOptions));
    }

    /// <summary>
    /// Calculates a complete VFR navigation log with all flight planning data.
    /// Requires all performance data and route information.
    /// </summary>
    [McpServerTool]
    [Description("Calculate a complete VFR navigation log. Computes course, heading (wind-corrected), ground speed, fuel burn, and time for each leg. Also identifies airspaces and obstacles along the route. IMPORTANT: Call validate_flight_plan_inputs first to ensure all required data is available.")]
    public async Task<string> CalculateNavlog(
        [Description("Departure airport code (ICAO or FAA identifier)")] string departureAirport,
        [Description("Destination airport code (ICAO or FAA identifier)")] string destinationAirport,
        [Description("Intermediate waypoint codes, comma-separated (optional)")] string? intermediateWaypoints,
        [Description("Departure time in UTC (ISO 8601 format)")] string departureTimeUtc,
        [Description("Cruising altitude in feet MSL")] int cruisingAltitude,
        [Description("Cruise true airspeed in knots")] int cruiseTas,
        [Description("Climb true airspeed in knots")] int climbTas,
        [Description("Descent true airspeed in knots")] int descentTas,
        [Description("Climb rate in feet per minute")] int climbRate,
        [Description("Descent rate in feet per minute")] int descentRate,
        [Description("Cruise fuel burn in gallons per hour")] double cruiseFuelBurn,
        [Description("Climb fuel burn in gallons per hour")] double climbFuelBurn,
        [Description("Descent fuel burn in gallons per hour")] double descentFuelBurn,
        [Description("Start/taxi/takeoff fuel in gallons")] double sttFuel,
        [Description("Total fuel on board at departure in gallons")] double fuelOnBoard)
    {
        // Parse departure time
        if (!DateTime.TryParse(departureTimeUtc, out var departureTime))
        {
            return JsonSerializer.Serialize(
                McpToolResponse<object>.Fail($"Invalid departure time format: {departureTimeUtc}. Use ISO 8601 format (e.g., 2024-01-15T14:00:00Z)"),
                JsonOptions);
        }

        // Get airport coordinates
        var (depAirport, depError) = await _apiClient.GetAirportAsync(departureAirport);
        if (depError != null || depAirport == null)
        {
            return JsonSerializer.Serialize(
                McpToolResponse<object>.Fail($"Could not find departure airport: {depError ?? "Unknown error"}"),
                JsonOptions);
        }

        var (destAirport, destError) = await _apiClient.GetAirportAsync(destinationAirport);
        if (destError != null || destAirport == null)
        {
            return JsonSerializer.Serialize(
                McpToolResponse<object>.Fail($"Could not find destination airport: {destError ?? "Unknown error"}"),
                JsonOptions);
        }

        // Build waypoints list
        var waypoints = new List<WaypointRequest>
        {
            new()
            {
                Id = depAirport.IcaoId ?? depAirport.ArptId ?? departureAirport,
                Name = depAirport.ArptName ?? departureAirport,
                Latitude = (double)(depAirport.LatDecimal ?? 0),
                Longitude = (double)(depAirport.LongDecimal ?? 0),
                Altitude = (double)(depAirport.Elev ?? 0),
                WaypointType = "Airport"
            }
        };

        // Add intermediate waypoints if provided
        if (!string.IsNullOrWhiteSpace(intermediateWaypoints))
        {
            var wpCodes = intermediateWaypoints.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var wpCode in wpCodes)
            {
                var (wp, wpError) = await _apiClient.GetAirportAsync(wpCode);
                if (wp != null)
                {
                    waypoints.Add(new WaypointRequest
                    {
                        Id = wp.IcaoId ?? wp.ArptId ?? wpCode,
                        Name = wp.ArptName ?? wpCode,
                        Latitude = (double)(wp.LatDecimal ?? 0),
                        Longitude = (double)(wp.LongDecimal ?? 0),
                        Altitude = cruisingAltitude, // Intermediate waypoints at cruise altitude
                        WaypointType = "Airport"
                    });
                }
            }
        }

        // Add destination
        waypoints.Add(new WaypointRequest
        {
            Id = destAirport.IcaoId ?? destAirport.ArptId ?? destinationAirport,
            Name = destAirport.ArptName ?? destinationAirport,
            Latitude = (double)(destAirport.LatDecimal ?? 0),
            Longitude = (double)(destAirport.LongDecimal ?? 0),
            Altitude = (double)(destAirport.Elev ?? 0),
            WaypointType = "Airport"
        });

        // Build request
        var request = new NavlogRequest
        {
            Waypoints = waypoints,
            PlannedCruisingAltitude = cruisingAltitude,
            TimeOfDeparture = departureTime.ToUniversalTime(),
            PerformanceData = new PerformanceDataRequest
            {
                ClimbTrueAirspeed = climbTas,
                CruiseTrueAirspeed = cruiseTas,
                DescentTrueAirspeed = descentTas,
                ClimbFpm = climbRate,
                DescentFpm = descentRate,
                ClimbFuelBurn = climbFuelBurn,
                CruiseFuelBurn = cruiseFuelBurn,
                DescentFuelBurn = descentFuelBurn,
                SttFuelGals = sttFuel,
                FuelOnBoardGals = fuelOnBoard
            }
        };

        var (navlog, error) = await _apiClient.CalculateNavlogAsync(request);

        if (error != null || navlog == null)
        {
            return JsonSerializer.Serialize(
                McpToolResponse<object>.Fail($"Navigation log calculation failed: {error ?? "Unknown error"}"),
                JsonOptions);
        }

        var uncertainties = new List<UncertaintyItem>();

        // Calculate fuel reserve
        var lastLeg = navlog.Legs.LastOrDefault();
        var fuelRemaining = lastLeg?.RemainingFuelGals ?? fuelOnBoard - navlog.TotalFuelUsed;
        var reserveMinutes = (fuelRemaining / cruiseFuelBurn) * 60;

        if (reserveMinutes < 30)
        {
            uncertainties.Add(new UncertaintyItem
            {
                Type = UncertaintyType.FuelMarginWarning,
                Message = $"Critical: Only {reserveMinutes:F0} minutes of fuel reserve at destination",
                SuggestedPrompt = "Fuel reserve is below safe minimums. Would you like to add a fuel stop or reduce the route distance?",
                Severity = UncertaintySeverity.Critical
            });
        }
        else if (reserveMinutes < 45)
        {
            uncertainties.Add(new UncertaintyItem
            {
                Type = UncertaintyType.FuelMarginWarning,
                Message = $"Warning: Only {reserveMinutes:F0} minutes of fuel reserve at destination (recommended: 45+ minutes for VFR)",
                SuggestedPrompt = "Fuel reserve is lower than recommended. Would you like to consider adding a fuel stop?",
                Severity = UncertaintySeverity.Warning
            });
        }

        // Check for headwinds
        if (navlog.AverageWindComponent < -15)
        {
            uncertainties.Add(new UncertaintyItem
            {
                Type = UncertaintyType.WeatherConcern,
                Message = $"Significant headwind: average {Math.Abs(navlog.AverageWindComponent):F0}kt headwind component",
                Severity = UncertaintySeverity.Info
            });
        }

        // Check for airspace concerns
        if (navlog.AirspaceGlobalIds.Count > 0 || navlog.SpecialUseAirspaceGlobalIds.Count > 0)
        {
            var totalAirspaces = navlog.AirspaceGlobalIds.Count + navlog.SpecialUseAirspaceGlobalIds.Count;
            uncertainties.Add(new UncertaintyItem
            {
                Type = UncertaintyType.SafetyConcern,
                Message = $"Route passes through {totalAirspaces} airspace(s) - review airspace requirements",
                Severity = UncertaintySeverity.Info
            });
        }

        // Build simplified summary
        var legSummaries = navlog.Legs.Select((leg, index) => new LegSummary
        {
            LegNumber = index + 1,
            From = leg.LegStartPoint.Name,
            To = leg.LegEndPoint.Name,
            DistanceNm = Math.Round(leg.LegDistance, 1),
            MagneticHeading = (int)Math.Round(leg.MagneticHeading),
            GroundSpeed = (int)Math.Round(leg.GroundSpeed),
            EteMinutes = (int)Math.Round((leg.EndLegTime - leg.StartLegTime).TotalMinutes),
            FuelBurnGallons = Math.Round(leg.LegFuelBurnGals, 1)
        }).ToList();

        var summary = new NavlogSummary
        {
            TotalDistanceNm = Math.Round(navlog.TotalRouteDistance, 1),
            TotalTimeHours = Math.Round(navlog.TotalRouteTimeHours, 2),
            TotalFuelGallons = Math.Round(navlog.TotalFuelUsed, 1),
            FuelRemainingGallons = Math.Round(fuelRemaining, 1),
            ReserveMinutes = Math.Round(reserveMinutes, 0),
            AverageWindComponent = Math.Round(navlog.AverageWindComponent, 1),
            AirspaceCount = navlog.AirspaceGlobalIds.Count + navlog.SpecialUseAirspaceGlobalIds.Count,
            ObstacleCount = navlog.ObstacleOasNumbers.Count,
            Legs = legSummaries
        };

        var summaryText = $"Route: {departureAirport} to {destinationAirport}, " +
                          $"Distance: {summary.TotalDistanceNm}nm, " +
                          $"Time: {summary.TotalTimeHours:F1}hrs, " +
                          $"Fuel: {summary.TotalFuelGallons}gal (reserve: {summary.ReserveMinutes}min)";

        var response = McpToolResponse<NavlogSummary>.Ok(summary, summaryText, uncertainties);
        return JsonSerializer.Serialize(response, JsonOptions);
    }
}

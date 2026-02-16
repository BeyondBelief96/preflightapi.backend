namespace PreflightApi.MCP.Models;

/// <summary>
/// Input for validating flight plan completeness.
/// </summary>
public record FlightPlanValidationInput
{
    /// <summary>Departure airport code (ICAO or FAA identifier).</summary>
    public string? DepartureAirport { get; init; }

    /// <summary>Destination airport code (ICAO or FAA identifier).</summary>
    public string? DestinationAirport { get; init; }

    /// <summary>Intermediate waypoints (optional).</summary>
    public List<string>? IntermediateWaypoints { get; init; }

    /// <summary>Planned departure time in UTC.</summary>
    public DateTime? DepartureTimeUtc { get; init; }

    /// <summary>Planned cruising altitude in feet MSL.</summary>
    public int? CruisingAltitude { get; init; }

    /// <summary>Aircraft cruise true airspeed in knots.</summary>
    public int? CruiseTas { get; init; }

    /// <summary>Aircraft climb true airspeed in knots.</summary>
    public int? ClimbTas { get; init; }

    /// <summary>Aircraft descent true airspeed in knots.</summary>
    public int? DescentTas { get; init; }

    /// <summary>Rate of climb in feet per minute.</summary>
    public int? ClimbRate { get; init; }

    /// <summary>Rate of descent in feet per minute.</summary>
    public int? DescentRate { get; init; }

    /// <summary>Cruise fuel burn rate in gallons per hour.</summary>
    public double? CruiseFuelBurn { get; init; }

    /// <summary>Climb fuel burn rate in gallons per hour.</summary>
    public double? ClimbFuelBurn { get; init; }

    /// <summary>Descent fuel burn rate in gallons per hour.</summary>
    public double? DescentFuelBurn { get; init; }

    /// <summary>Start/taxi/takeoff fuel in gallons.</summary>
    public double? SttFuel { get; init; }

    /// <summary>Total fuel on board at departure in gallons.</summary>
    public double? FuelOnBoard { get; init; }
}

/// <summary>
/// VFR weather assessment result for a single airport.
/// </summary>
public record VfrWeatherAssessment
{
    /// <summary>Airport identifier.</summary>
    public required string AirportCode { get; init; }

    /// <summary>Flight category (VFR, MVFR, IFR, LIFR).</summary>
    public required string FlightCategory { get; init; }

    /// <summary>GO/CAUTION/NO-GO assessment.</summary>
    public required string Assessment { get; init; }

    /// <summary>Visibility in statute miles.</summary>
    public string? Visibility { get; init; }

    /// <summary>Ceiling in feet AGL (lowest BKN/OVC layer).</summary>
    public int? CeilingFeet { get; init; }

    /// <summary>Wind information.</summary>
    public string? Wind { get; init; }

    /// <summary>Weather phenomena (rain, fog, etc.).</summary>
    public string? Weather { get; init; }

    /// <summary>Observation time.</summary>
    public string? ObservationTime { get; init; }

    /// <summary>Raw METAR text.</summary>
    public string? RawMetar { get; init; }

    /// <summary>Concerns identified with this weather.</summary>
    public List<string> Concerns { get; init; } = [];
}

/// <summary>
/// Combined VFR weather assessment for a route.
/// </summary>
public record RouteWeatherAssessment
{
    /// <summary>Overall route assessment (GO/CAUTION/NO-GO).</summary>
    public required string OverallAssessment { get; init; }

    /// <summary>Individual airport assessments.</summary>
    public List<VfrWeatherAssessment> Airports { get; init; } = [];

    /// <summary>Summary of weather conditions.</summary>
    public string? Summary { get; init; }
}

/// <summary>
/// Simplified navlog summary for MCP response.
/// </summary>
public record NavlogSummary
{
    /// <summary>Total route distance in nautical miles.</summary>
    public double TotalDistanceNm { get; init; }

    /// <summary>Total estimated time in hours.</summary>
    public double TotalTimeHours { get; init; }

    /// <summary>Total fuel used in gallons.</summary>
    public double TotalFuelGallons { get; init; }

    /// <summary>Fuel remaining at destination in gallons.</summary>
    public double FuelRemainingGallons { get; init; }

    /// <summary>Estimated time at destination reserve in minutes.</summary>
    public double ReserveMinutes { get; init; }

    /// <summary>Average wind component (negative = headwind).</summary>
    public double AverageWindComponent { get; init; }

    /// <summary>Number of airspaces along route.</summary>
    public int AirspaceCount { get; init; }

    /// <summary>Number of obstacles along route.</summary>
    public int ObstacleCount { get; init; }

    /// <summary>Leg summaries.</summary>
    public List<LegSummary> Legs { get; init; } = [];
}

/// <summary>
/// Simplified leg summary.
/// </summary>
public record LegSummary
{
    /// <summary>Leg number (1-based).</summary>
    public int LegNumber { get; init; }

    /// <summary>From waypoint.</summary>
    public required string From { get; init; }

    /// <summary>To waypoint.</summary>
    public required string To { get; init; }

    /// <summary>Distance in nautical miles.</summary>
    public double DistanceNm { get; init; }

    /// <summary>Magnetic heading to fly.</summary>
    public int MagneticHeading { get; init; }

    /// <summary>Ground speed in knots.</summary>
    public int GroundSpeed { get; init; }

    /// <summary>Estimated time enroute for this leg in minutes.</summary>
    public int EteMinutes { get; init; }

    /// <summary>Fuel burn for this leg in gallons.</summary>
    public double FuelBurnGallons { get; init; }
}

using System.Text.Json.Serialization;

namespace PreflightApi.MCP.Models;

/// <summary>
/// Simplified airport data returned from the API.
/// </summary>
public record AirportResponse
{
    public string SiteNo { get; init; } = string.Empty;
    public string? IcaoId { get; init; }
    public string? ArptId { get; init; }
    public string? ArptName { get; init; }
    public string? City { get; init; }
    public string? StateCode { get; init; }
    public decimal? LatDecimal { get; init; }
    public decimal? LongDecimal { get; init; }
    public decimal? Elev { get; init; }
    public string? FuelTypes { get; init; }
    public string? TwrTypeCode { get; init; }
    public bool NotamAvailable { get; init; }
    public int? Tpa { get; init; }
}

/// <summary>
/// Paginated API response wrapper.
/// </summary>
public record PaginatedResponse<T>
{
    public List<T> Data { get; init; } = [];
    public string? NextCursor { get; init; }
    public int TotalCount { get; init; }
}

/// <summary>
/// Sky condition layer in a METAR.
/// </summary>
public record SkyConditionResponse
{
    public string SkyCover { get; init; } = string.Empty;
    public int? CloudBaseFtAgl { get; init; }
}

/// <summary>
/// METAR observation data.
/// </summary>
public record MetarResponse
{
    public string? RawText { get; init; }
    public string? StationId { get; init; }
    public string? ObservationTime { get; init; }
    public float? TempC { get; init; }
    public float? DewpointC { get; init; }
    public string? WindDirDegrees { get; init; }
    public int? WindSpeedKt { get; init; }
    public int? WindGustKt { get; init; }
    public string? VisibilityStatuteMi { get; init; }
    public float? AltimInHg { get; init; }
    public string? WxString { get; init; }
    public List<SkyConditionResponse>? SkyCondition { get; init; }
    public string? FlightCategory { get; init; }
}

/// <summary>
/// TAF forecast period.
/// </summary>
public record TafForecastResponse
{
    public string? FcstTimeFrom { get; init; }
    public string? FcstTimeTo { get; init; }
    public string? ChangeIndicator { get; init; }
    public string? WindDirDegrees { get; init; }
    public int? WindSpeedKt { get; init; }
    public int? WindGustKt { get; init; }
    public string? VisibilityStatuteMi { get; init; }
    public string? WxString { get; init; }
    public List<TafSkyConditionResponse>? SkyConditions { get; init; }
}

/// <summary>
/// TAF sky condition.
/// </summary>
public record TafSkyConditionResponse
{
    public string? SkyCover { get; init; }
    public int? CloudBaseFtAgl { get; init; }
}

/// <summary>
/// TAF forecast data.
/// </summary>
public record TafResponse
{
    public string? RawText { get; init; }
    public string? StationId { get; init; }
    public string? IssueTime { get; init; }
    public string? ValidTimeFrom { get; init; }
    public string? ValidTimeTo { get; init; }
    public List<TafForecastResponse>? Forecast { get; init; }
}

/// <summary>
/// Waypoint for navigation calculations.
/// </summary>
public record WaypointRequest
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public double Altitude { get; init; }
    public string? WaypointType { get; init; }
    public double? RefuelGallons { get; init; }
    public bool? RefuelToFull { get; init; }
    public bool? IsRefuelingStop { get; init; }
}

/// <summary>
/// Aircraft performance data for navigation log calculations.
/// </summary>
public record PerformanceDataRequest
{
    public int ClimbTrueAirspeed { get; init; }
    public int CruiseTrueAirspeed { get; init; }
    public int DescentTrueAirspeed { get; init; }
    public int ClimbFpm { get; init; }
    public int DescentFpm { get; init; }
    public double ClimbFuelBurn { get; init; }
    public double CruiseFuelBurn { get; init; }
    public double DescentFuelBurn { get; init; }
    public double SttFuelGals { get; init; }
    public double FuelOnBoardGals { get; init; }
}

/// <summary>
/// Navigation log calculation request.
/// </summary>
public record NavlogRequest
{
    public List<WaypointRequest> Waypoints { get; init; } = [];
    public PerformanceDataRequest PerformanceData { get; init; } = new();
    public int PlannedCruisingAltitude { get; init; }
    public DateTime TimeOfDeparture { get; init; }
}

/// <summary>
/// Navigation leg response.
/// </summary>
public record NavigationLegResponse
{
    public WaypointRequest LegStartPoint { get; init; } = new();
    public WaypointRequest LegEndPoint { get; init; } = new();
    public double TrueCourse { get; init; }
    public double MagneticHeading { get; init; }
    public double MagneticCourse { get; init; }
    public double GroundSpeed { get; init; }
    public double LegDistance { get; init; }
    public double DistanceRemaining { get; init; }
    public DateTime StartLegTime { get; init; }
    public DateTime EndLegTime { get; init; }
    public double LegFuelBurnGals { get; init; }
    public double RemainingFuelGals { get; init; }
    public int WindDir { get; init; }
    public int WindSpeed { get; init; }
    public double HeadwindComponent { get; init; }
    public float TempC { get; init; }
}

/// <summary>
/// Navigation log calculation response.
/// </summary>
public record NavlogResponse
{
    public double TotalRouteDistance { get; init; }
    public double TotalRouteTimeHours { get; init; }
    public double TotalFuelUsed { get; init; }
    public double AverageWindComponent { get; init; }
    public List<NavigationLegResponse> Legs { get; init; } = [];
    public List<string> AirspaceGlobalIds { get; init; } = [];
    public List<string> SpecialUseAirspaceGlobalIds { get; init; } = [];
    public List<string> ObstacleOasNumbers { get; init; } = [];
}

/// <summary>
/// API error response.
/// </summary>
public record ApiErrorResponse
{
    public string? Type { get; init; }
    public string? Title { get; init; }
    public int Status { get; init; }
    public string? Detail { get; init; }
}

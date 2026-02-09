namespace PreflightApi.Domain.Exceptions;

/// <summary>
/// Centralized error codes for consistent error identification across the API.
/// </summary>
public static class ErrorCodes
{
    // General
    public const string InternalError = "INTERNAL_ERROR";
    public const string ValidationError = "VALIDATION_ERROR";
    public const string NotFound = "NOT_FOUND";
    public const string Conflict = "CONFLICT";

    // Airports
    public const string AirportNotFound = "AIRPORT_NOT_FOUND";
    public const string AirportDiagramNotFound = "AIRPORT_DIAGRAM_NOT_FOUND";
    public const string RunwayNotFound = "RUNWAY_NOT_FOUND";
    public const string CommunicationFrequencyNotFound = "COMMUNICATION_FREQUENCY_NOT_FOUND";

    // Obstacles
    public const string ObstacleNotFound = "OBSTACLE_NOT_FOUND";

    // Weather
    public const string MetarNotFound = "METAR_NOT_FOUND";
    public const string TafNotFound = "TAF_NOT_FOUND";
    public const string WeatherServiceUnavailable = "WEATHER_SERVICE_UNAVAILABLE";
    public const string WeatherDataMissing = "WEATHER_DATA_MISSING";

    // NOTAMs
    public const string NotamServiceUnavailable = "NOTAM_SERVICE_UNAVAILABLE";

    // Chart Supplements
    public const string ChartSupplementNotFound = "CHART_SUPPLEMENT_NOT_FOUND";

    // Airspace
    public const string AirspaceNotFound = "AIRSPACE_NOT_FOUND";

    // External Services
    public const string ExternalServiceUnavailable = "EXTERNAL_SERVICE_UNAVAILABLE";

    // Performance Calculations
    public const string PerformanceCalculationError = "PERFORMANCE_CALCULATION_ERROR";
    public const string InvalidPerformanceData = "INVALID_PERFORMANCE_DATA";

    // Navigation
    public const string NavlogCalculationError = "NAVLOG_CALCULATION_ERROR";

    // Navigational Aids
    public const string NavigationalAidNotFound = "NAVIGATIONAL_AID_NOT_FOUND";

    // Fixes
    public const string FixNotFound = "FIX_NOT_FOUND";

    // Weather Stations
    public const string WeatherStationNotFound = "WEATHER_STATION_NOT_FOUND";
}

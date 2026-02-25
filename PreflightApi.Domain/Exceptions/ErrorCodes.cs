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
    public const string Forbidden = "FORBIDDEN";

    // Airports
    public const string AirportNotFound = "AIRPORT_NOT_FOUND";
    public const string TerminalProcedureNotFound = "TERMINAL_PROCEDURE_NOT_FOUND";
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
    public const string NotamNotFound = "NOTAM_NOT_FOUND";
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

    // Gateway (used by APIM policies — keep string values in sync)
    public const string Unauthorized = "UNAUTHORIZED";
    public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
    public const string QuotaExceeded = "QUOTA_EXCEEDED";
    public const string TierRestricted = "TIER_RESTRICTED";
    public const string BackendUnavailable = "BACKEND_UNAVAILABLE";
    public const string Maintenance = "MAINTENANCE";
}

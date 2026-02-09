namespace PreflightApi.Domain.Exceptions;

/// <summary>
/// Base exception for when a requested resource cannot be found.
/// </summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string errorCode, string userMessage, string? devMessage = null)
        : base(errorCode, userMessage, devMessage)
    {
    }

    public NotFoundException(string resourceType, object resourceId)
        : base(ErrorCodes.NotFound, $"{resourceType} was not found with ID '{resourceId}'.")
    {
    }
}

/// <summary>
/// Exception thrown when an airport cannot be found.
/// </summary>
public class AirportNotFoundException : NotFoundException
{
    public AirportNotFoundException(string identifier)
        : base(ErrorCodes.AirportNotFound, $"Airport was not found with identifier '{identifier}'.")
    {
    }
}

/// <summary>
/// Exception thrown when METAR data cannot be found.
/// </summary>
public class MetarNotFoundException : NotFoundException
{
    public MetarNotFoundException(string stationId)
        : base(ErrorCodes.MetarNotFound, $"METAR data was not found for station '{stationId}'.")
    {
    }

    public MetarNotFoundException(IEnumerable<string> stationIds)
        : base(ErrorCodes.MetarNotFound, $"METAR data was not found for stations: {string.Join(", ", stationIds)}.")
    {
    }
}

/// <summary>
/// Exception thrown when TAF data cannot be found.
/// </summary>
public class TafNotFoundException : NotFoundException
{
    public TafNotFoundException(string stationId)
        : base(ErrorCodes.TafNotFound, $"TAF data was not found for station '{stationId}'.")
    {
    }

    public TafNotFoundException(IEnumerable<string> stationIds)
        : base(ErrorCodes.TafNotFound, $"TAF data was not found for stations: {string.Join(", ", stationIds)}.")
    {
    }
}

/// <summary>
/// Exception thrown when an airport diagram cannot be found.
/// </summary>
public class AirportDiagramNotFoundException : NotFoundException
{
    public AirportDiagramNotFoundException(string airportId)
        : base(ErrorCodes.AirportDiagramNotFound, $"Airport diagram was not found for airport '{airportId}'.")
    {
    }
}

/// <summary>
/// Exception thrown when a chart supplement cannot be found.
/// </summary>
public class ChartSupplementNotFoundException : NotFoundException
{
    public ChartSupplementNotFoundException(string airportId)
        : base(ErrorCodes.ChartSupplementNotFound, $"Chart supplement was not found for airport '{airportId}'.")
    {
    }
}

/// <summary>
/// Exception thrown when an obstacle cannot be found.
/// </summary>
public class ObstacleNotFoundException : NotFoundException
{
    public ObstacleNotFoundException(string oasNumber)
        : base(ErrorCodes.ObstacleNotFound, $"Obstacle was not found with OAS number '{oasNumber}'.")
    {
    }
}

/// <summary>
/// Exception thrown when a runway cannot be found.
/// </summary>
public class RunwayNotFoundException : NotFoundException
{
    public RunwayNotFoundException(string airportId)
        : base(ErrorCodes.RunwayNotFound, $"Runways were not found for airport '{airportId}'.")
    {
    }
}

/// <summary>
/// Exception thrown when a navigational aid cannot be found.
/// </summary>
public class NavigationalAidNotFoundException : NotFoundException
{
    public NavigationalAidNotFoundException(string identifier)
        : base(ErrorCodes.NavigationalAidNotFound, $"Navigational aid was not found with identifier '{identifier}'.")
    {
    }
}

/// <summary>
/// Exception thrown when a fix/reporting point cannot be found.
/// </summary>
public class FixNotFoundException : NotFoundException
{
    public FixNotFoundException(string identifier)
        : base(ErrorCodes.FixNotFound, $"Fix was not found with identifier '{identifier}'.")
    {
    }
}

/// <summary>
/// Exception thrown when a weather station cannot be found.
/// </summary>
public class WeatherStationNotFoundException : NotFoundException
{
    public WeatherStationNotFoundException(string identifier)
        : base(ErrorCodes.WeatherStationNotFound, $"Weather station was not found with identifier '{identifier}'.")
    {
    }
}

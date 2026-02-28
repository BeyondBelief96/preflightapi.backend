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
/// Exception thrown when terminal procedures cannot be found.
/// </summary>
public class TerminalProcedureNotFoundException : NotFoundException
{
    public TerminalProcedureNotFoundException(string airportId)
        : base(ErrorCodes.TerminalProcedureNotFound, $"Terminal procedures were not found for airport '{airportId}'.")
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
/// Exception thrown when a navaid cannot be found by identifier.
/// </summary>
public class NavaidNotFoundException : NotFoundException
{
    public NavaidNotFoundException(string navId)
        : base(ErrorCodes.NavaidNotFound, $"No navaids were found with identifier '{navId}'.")
    {
    }
}

/// <summary>
/// Exception thrown when a NOTAM cannot be found by NMS ID.
/// </summary>
public class NotamNotFoundException : NotFoundException
{
    public NotamNotFoundException(string nmsId)
        : base(ErrorCodes.NotamNotFound, $"NOTAM was not found with NMS ID '{nmsId}'.")
    {
    }
}

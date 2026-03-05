using PreflightApi.Domain.ValueObjects.Taf;

namespace PreflightApi.Infrastructure.Dtos;

/// <summary>
/// TAF (Terminal Aerodrome Forecast) data for an airport.
/// </summary>
public record TafDto
{
    /// <summary>Raw TAF forecast text as received from the source. ex: KORD 061728Z 0618/0724 21012KT P6SM BKN250</summary>
    public string? RawText { get; init; }
    /// <summary>ICAO station identifier (e.g., KDFW).</summary>
    public string? StationId { get; init; }
    /// <summary>Time the TAF was issued in ISO 8601 format.</summary>
    public string? IssueTime { get; init; }
    /// <summary>Bulletin time in ISO 8601 format.</summary>
    public string? BulletinTime { get; init; }
    /// <summary>Start of the TAF valid period in ISO 8601 format.</summary>
    public string? ValidTimeFrom { get; init; }
    /// <summary>End of the TAF valid period in ISO 8601 format.</summary>
    public string? ValidTimeTo { get; init; }
    /// <summary>TAF remarks.</summary>
    public string? Remarks { get; init; }
    /// <summary>Station latitude in decimal degrees.</summary>
    public double? Latitude { get; init; }
    /// <summary>Station longitude in decimal degrees.</summary>
    public double? Longitude { get; init; }
    /// <summary>Station elevation in meters.</summary>
    public double? ElevationM { get; init; }
    /// <summary>Forecast periods within the TAF.</summary>
    public List<TafForecast>? Forecast { get; init; }
}

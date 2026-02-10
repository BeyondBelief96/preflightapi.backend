namespace PreflightApi.Infrastructure.Dtos.Notam;

/// <summary>
/// Response containing NOTAMs (Notices to Air Missions) matching a query.
/// NOTAMs are returned as GeoJSON Features from the FAA NOTAM Search (NMS) system.
/// </summary>
public record NotamResponseDto
{
    /// <summary>List of NOTAMs matching the query, each represented as a GeoJSON Feature with geometry and properties.</summary>
    public List<NotamDto> Notams { get; init; } = [];
    /// <summary>Total number of NOTAMs returned in this response.</summary>
    public int TotalCount { get; init; }
    /// <summary>UTC timestamp when the NOTAMs were retrieved from the FAA NMS system.</summary>
    public DateTime RetrievedAt { get; init; } = DateTime.UtcNow;
    /// <summary>Description of the queried location (e.g., airport identifier, coordinates, or route description).</summary>
    public string? QueryLocation { get; init; }
}

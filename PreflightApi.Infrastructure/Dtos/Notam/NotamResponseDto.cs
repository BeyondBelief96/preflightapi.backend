namespace PreflightApi.Infrastructure.Dtos.Notam;

/// <summary>
/// Response containing NOTAMs (Notices to Air Missions) matching a query.
/// Each NOTAM is a GeoJSON Feature with geographic geometry and detailed properties.
/// Used by the airport, radius, and route endpoints. The search endpoint uses cursor-based pagination instead.
/// </summary>
public record NotamResponseDto
{
    /// <summary>NOTAMs matching the query, each as a GeoJSON Feature with geometry and properties.</summary>
    public List<NotamDto> Notams { get; init; } = [];
    /// <summary>Total number of NOTAMs returned in this response.</summary>
    public int TotalCount { get; init; }
    /// <summary>UTC timestamp when the query was executed.</summary>
    public DateTime RetrievedAt { get; init; } = DateTime.UtcNow;
    /// <summary>Description of the queried location (e.g., "KDFW", "32.8970,-97.0380 (25nm)", or "KDFW -&gt; KAUS").</summary>
    public string? QueryLocation { get; init; }
}

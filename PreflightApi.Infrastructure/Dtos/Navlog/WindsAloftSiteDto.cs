namespace PreflightApi.Infrastructure.Dtos.Navlog;

/// <summary>
/// Winds aloft data for a single reporting site.
/// </summary>
public record WindsAloftSiteDto
{
    /// <summary>Site identifier (e.g., DFW, ABI).</summary>
    public string Id { get; init; } = string.Empty;
    /// <summary>Site latitude in decimal degrees (WGS 84).</summary>
    public float Lat { get; init; }
    /// <summary>Site longitude in decimal degrees (WGS 84).</summary>
    public float Lon { get; init; }
    /// <summary>Wind and temperature data keyed by altitude level (e.g., "3000", "6000").</summary>
    public Dictionary<string, WindTempDto> WindTemp { get; init; } = new();
}

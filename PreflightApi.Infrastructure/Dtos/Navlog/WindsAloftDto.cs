namespace PreflightApi.Infrastructure.Dtos.Navlog;

/// <summary>
/// Winds aloft forecast data for a specific forecast period.
/// </summary>
public record WindsAloftDto
{
    /// <summary>Valid time for the forecast data.</summary>
    public DateTime ValidTime { get; init; }
    /// <summary>Start of the forecast use period.</summary>
    public DateTime ForUseStartTime { get; init; }
    /// <summary>End of the forecast use period.</summary>
    public DateTime ForUseEndTime { get; init; }
    /// <summary>Wind and temperature data for each reporting site.</summary>
    public List<WindsAloftSiteDto> WindTemp  { get; init; } = [];
}

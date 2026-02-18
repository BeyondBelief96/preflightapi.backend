using PreflightApi.Domain.ValueObjects.Sigmets;

namespace PreflightApi.Infrastructure.Dtos;

/// <summary>
/// Domestic SIGMET advisory data including hazard information and affected area.
/// </summary>
public record SigmetDto
{
    /// <summary>Database identifier.</summary>
    public int Id { get; init; }
    /// <summary>Raw SIGMET text.</summary>
    public string? RawText { get; init; }
    /// <summary>Start of the valid period in ISO 8601 format.</summary>
    public string? ValidTimeFrom { get; init; }
    /// <summary>End of the valid period in ISO 8601 format.</summary>
    public string? ValidTimeTo { get; init; }
    /// <summary>Altitude range of the advisory.</summary>
    public SigmetAltitude? Altitude { get; init; }
    /// <summary>Movement direction in degrees true.</summary>
    public int? MovementDirDegrees { get; init; }
    /// <summary>Movement speed in knots.</summary>
    public int? MovementSpeedKt { get; init; }
    /// <summary>Hazard type and severity information.</summary>
    public SigmetHazardDto? Hazard { get; init; }
    /// <summary>Advisory type: always SIGMET.</summary>
    public string? SigmetType { get; init; }
    /// <summary>Geographic areas affected by the advisory.</summary>
    public List<SigmetArea>? Areas { get; init; }
}

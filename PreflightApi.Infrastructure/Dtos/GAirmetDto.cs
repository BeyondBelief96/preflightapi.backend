using PreflightApi.Domain.Enums;
using PreflightApi.Domain.ValueObjects.GAirmets;

namespace PreflightApi.Infrastructure.Dtos;

public record GAirmetDto
{
    public int Id { get; init; }
    public DateTime ReceiptTime { get; init; }
    public DateTime IssueTime { get; init; }
    public DateTime ExpireTime { get; init; }
    public DateTime ValidTime { get; init; }
    public GAirmetProduct Product { get; init; }
    public string? Tag { get; init; }
    public int ForecastHour { get; init; }
    public GAirmetHazardType? Hazard { get; init; }
    public string? HazardSeverity { get; init; }
    public string? GeometryType { get; init; }
    public string? DueTo { get; init; }
    public List<GAirmetAltitude>? Altitudes { get; init; }
    public GAirmetArea? Area { get; init; }
}

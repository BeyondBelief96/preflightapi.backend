using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RunwayEdgeLightIntensity
{
    Unknown,
    None,
    High,
    Medium,
    Low,
    Flood,
    NonStandard,
    Perimeter,
    Strobe
}

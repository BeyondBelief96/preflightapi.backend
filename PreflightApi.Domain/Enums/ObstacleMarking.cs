using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ObstacleMarking
{
    Unknown,
    OrangeOrOrangeWhitePaint,
    WhitePaintOnly,
    Marked,
    FlagMarker,
    SphericalMarker,
    None
}

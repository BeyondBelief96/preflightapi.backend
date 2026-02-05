using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ObstacleLighting
{
    Unknown,
    Red,
    DualMediumWhiteStrobeRed,
    HighIntensityWhiteStrobeRed,
    MediumIntensityWhiteStrobe,
    HighIntensityWhiteStrobe,
    Flood,
    DualMediumCatenary,
    SynchronizedRedLighting,
    Lighted,
    None
}

using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VerticalAccuracy
{
    Within3Feet,
    Within10Feet,
    Within20Feet,
    Within50Feet,
    Within125Feet,
    Within250Feet,
    Within500Feet,
    Within1000Feet,
    Unknown
}

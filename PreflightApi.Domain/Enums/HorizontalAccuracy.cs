using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HorizontalAccuracy
{
    Within20Feet,
    Within50Feet,
    Within100Feet,
    Within250Feet,
    Within500Feet,
    Within1000Feet,
    WithinHalfNauticalMile,
    Within1NauticalMile,
    Unknown
}

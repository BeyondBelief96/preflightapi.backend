using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RunwayMarkingsType
{
    Unknown,
    None,
    PrecisionInstrument,
    NonPrecisionInstrument,
    Basic,
    NumbersOnly,
    NonStandard,
    Buoys,
    Stol
}

using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SkyCover
{
    SKC,
    CLR,
    FEW,
    SCT,
    BKN,
    OVC,
    OVX
}

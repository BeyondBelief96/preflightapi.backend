using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RunwayMarkingsCondition
{
    Unknown,
    Good,
    Fair,
    Poor
}

using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HazardSeverity
{
    LGT,
    LT_MOD,
    MOD,
    MOD_SEV,
    SEV
}

using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NotamClassification
{
    INTERNATIONAL,
    MILITARY,
    LOCAL_MILITARY,
    DOMESTIC,
    FDC
}

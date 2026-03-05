using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PirepReportType
{
    UA,
    UUA,
    PIREP,
    AIREP
}

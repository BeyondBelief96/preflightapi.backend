using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AirspeedUnits
{
    Knots,
    MPH,
    KPH
}

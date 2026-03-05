using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FlightCategory
{
    VFR,
    MVFR,
    IFR,
    LIFR
}

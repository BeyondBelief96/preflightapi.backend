using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NotamFeature
{
    RWY,
    TWY,
    APRON,
    AD,
    OBST,
    NAV,
    COM,
    SVC,
    AIRSPACE,
    ODP,
    SID,
    STAR,
    CHART,
    DATA,
    DVA,
    IAP,
    VFP,
    ROUTE,
    SPECIAL,
    SECURITY
}

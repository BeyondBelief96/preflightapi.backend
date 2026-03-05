using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TerminalProcedureChartCode
{
    IAP,
    DP,
    STAR,
    APD,
    MIN,
    HOT,
    DAU,
    LAH,
    ODP
}

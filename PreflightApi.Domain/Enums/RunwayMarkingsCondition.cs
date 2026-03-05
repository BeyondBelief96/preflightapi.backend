using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Runway Markings Condition. Corresponds to FAA NASR field RWY_MARKING_COND (APT_RWY_END).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RunwayMarkingsCondition
{
    /// <summary>G - Good condition.</summary>
    Good,

    /// <summary>F - Fair condition.</summary>
    Fair,

    /// <summary>P - Poor condition.</summary>
    Poor
}

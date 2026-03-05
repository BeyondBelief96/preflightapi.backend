using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Runway Surface Condition (COND). Corresponds to FAA NASR field SURFACE_COND (APT_RWY).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RunwaySurfaceCondition
{
    Excellent,
    Good,
    Fair,
    Poor,
    Failed
}

using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Subgrade Strength. Corresponds to FAA NASR field SUBGRADE_STRENGTH_CODE (APT_RWY).
/// Part of the Pavement Classification Number (PCN) system.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SubgradeStrength
{
    /// <summary>A - High strength.</summary>
    High,

    /// <summary>B - Medium strength.</summary>
    Medium,

    /// <summary>C - Low strength.</summary>
    Low,

    /// <summary>D - Ultra-low strength.</summary>
    UltraLow
}

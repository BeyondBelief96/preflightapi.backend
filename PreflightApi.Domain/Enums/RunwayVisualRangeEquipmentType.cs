using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Runway Visual Range (RVR) Equipment Location. Corresponds to FAA NASR field RWY_VISUAL_RANGE_EQUIP_CODE (APT_RWY_END).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RunwayVisualRangeEquipmentType
{
    /// <summary>RVR equipment type could not be determined from FAA data.</summary>
    Unknown,

    /// <summary>N - No RVR available.</summary>
    None,

    /// <summary>T - Touchdown.</summary>
    Touchdown,

    /// <summary>M - Midfield.</summary>
    Midfield,

    /// <summary>R - Rollout.</summary>
    Rollout,

    /// <summary>TM - Touchdown and Midfield.</summary>
    TouchdownMidfield,

    /// <summary>TR - Touchdown and Rollout.</summary>
    TouchdownRollout,

    /// <summary>MR - Midfield and Rollout.</summary>
    MidfieldRollout,

    /// <summary>TMR - Touchdown, Midfield, and Rollout.</summary>
    TouchdownMidfieldRollout
}

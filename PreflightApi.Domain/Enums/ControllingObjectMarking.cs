using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Controlling Object Marked/Lighted. Corresponds to FAA NASR field OBSTN_MRKD_CODE (APT_RWY_END).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ControllingObjectMarking
{
    /// <summary>Marking status could not be determined from FAA data.</summary>
    Unknown,

    /// <summary>NONE - Not marked or lighted.</summary>
    None,

    /// <summary>M - Marked.</summary>
    Marked,

    /// <summary>L - Lighted.</summary>
    Lighted,

    /// <summary>ML - Marked and Lighted.</summary>
    MarkedAndLighted
}

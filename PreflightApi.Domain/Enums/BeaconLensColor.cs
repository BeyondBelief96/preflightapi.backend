using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Airport Beacon Lens Color. Corresponds to FAA NASR field BCN_LENS_COLOR (APT_BASE).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BeaconLensColor
{
    /// <summary>Beacon lens color could not be determined from FAA data.</summary>
    Unknown,

    /// <summary>CG - Clear-Green (lighted land airport).</summary>
    ClearGreen,

    /// <summary>CY - Clear-Yellow (lighted water airport).</summary>
    ClearYellow,

    /// <summary>CGY - Clear-Green-Yellow (lighted heliport).</summary>
    ClearGreenYellow,

    /// <summary>SCG - Split Clear-Green (lighted military airport).</summary>
    SplitClearGreen,

    /// <summary>C - Clear (unlighted).</summary>
    Clear
}

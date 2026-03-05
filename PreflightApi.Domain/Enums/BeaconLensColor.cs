using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Airport Beacon Lens Color. Corresponds to FAA NASR field BCN_LENS_COLOR (APT_BASE).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BeaconLensColor
{
    /// <summary>WG - White-Green (lighted land airport).</summary>
    WhiteGreen,

    /// <summary>WY - White-Yellow (lighted seaplane base).</summary>
    WhiteYellow,

    /// <summary>WGY - White-Green-Yellow (heliport).</summary>
    WhiteGreenYellow,

    /// <summary>SWG - Split-White-Green (lighted military airport).</summary>
    SplitWhiteGreen,

    /// <summary>W - White (unlighted land airport).</summary>
    White,

    /// <summary>Y - Yellow (unlighted seaplane base).</summary>
    Yellow,

    /// <summary>G - Green (lighted land airport).</summary>
    Green,

    /// <summary>N - None (no beacon).</summary>
    None
}

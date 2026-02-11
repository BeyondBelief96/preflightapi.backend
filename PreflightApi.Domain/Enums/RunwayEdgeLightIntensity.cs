using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Runway Lights Edge Intensity. Corresponds to FAA NASR field RWY_LGT_CODE (APT_RWY).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RunwayEdgeLightIntensity
{
    /// <summary>Edge light intensity could not be determined from FAA data.</summary>
    Unknown,

    /// <summary>NONE - No edge lighting system.</summary>
    None,

    /// <summary>HIGH - High intensity edge lighting.</summary>
    High,

    /// <summary>MED - Medium intensity edge lighting.</summary>
    Medium,

    /// <summary>LOW - Low intensity edge lighting.</summary>
    Low,

    /// <summary>FLD - Flood lighting.</summary>
    Flood,

    /// <summary>NSTD - Non-standard lighting system.</summary>
    NonStandard,

    /// <summary>PERI - Perimeter lighting.</summary>
    Perimeter,

    /// <summary>STRB - Strobe lighting.</summary>
    Strobe
}

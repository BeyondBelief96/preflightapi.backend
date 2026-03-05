using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Wind Indicator Type. Corresponds to FAA NASR field WIND_INDCR_FLAG (APT_BASE).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WindIndicatorType
{
    /// <summary>N - No wind indicator.</summary>
    None,

    /// <summary>Y - Unlighted wind indicator exists.</summary>
    Unlighted,

    /// <summary>Y-L - Lighted wind indicator exists.</summary>
    Lighted
}

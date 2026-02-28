using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// VOR Standard Service Volume classification. Corresponds to FAA NASR field ALT_CODE (NAV1).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VorServiceVolume
{
    /// <summary>Service volume could not be determined from FAA data.</summary>
    Unknown,

    /// <summary>H - High altitude.</summary>
    High,

    /// <summary>L - Low altitude.</summary>
    Low,

    /// <summary>T - Terminal.</summary>
    Terminal,

    /// <summary>VH - VOR High altitude.</summary>
    VorHigh,

    /// <summary>VL - VOR Low altitude.</summary>
    VorLow
}

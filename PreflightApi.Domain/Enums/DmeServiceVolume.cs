using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// DME Standard Service Volume classification. Corresponds to FAA NASR field DME_SSV (NAV1).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DmeServiceVolume
{
    /// <summary>Service volume could not be determined from FAA data.</summary>
    Unknown,

    /// <summary>H - High altitude.</summary>
    High,

    /// <summary>L - Low altitude.</summary>
    Low,

    /// <summary>T - Terminal.</summary>
    Terminal,

    /// <summary>DH - DME High altitude.</summary>
    DmeHigh,

    /// <summary>DL - DME Low altitude.</summary>
    DmeLow
}

using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Approach Light System. Corresponds to FAA NASR field APCH_LGT_SYSTEM_CODE (APT_RWY_END).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApproachLightSystemType
{
    /// <summary>Approach light system could not be determined from FAA data.</summary>
    Unknown,

    /// <summary>NONE - No approach lighting is available.</summary>
    None,

    /// <summary>AFOVRN - Air Force Overrun 1,000-Foot Standard Approach Lighting System.</summary>
    AirForceOverrun,

    /// <summary>ALSAF - 3,000-Foot High Intensity Approach Lighting System with Centerline Sequence Flashers.</summary>
    Alsaf,

    /// <summary>ALSF1 - Standard 2,400-Foot High Intensity Approach Lighting System with Sequenced Flashers, Category I Configuration.</summary>
    Alsf1,

    /// <summary>ALSF2 - Standard 2,400-Foot High Intensity Approach Lighting System with Sequenced Flashers, Category II or III Configuration.</summary>
    Alsf2,

    /// <summary>MALS - 1,400-Foot Medium Intensity Approach Lighting System.</summary>
    Mals,

    /// <summary>MALSF - 1,400-Foot Medium Intensity Approach Lighting System with Sequenced Flashers.</summary>
    Malsf,

    /// <summary>MALSR - 1,400-Foot Medium Intensity Approach Lighting System with Runway Alignment Indicator Lights.</summary>
    Malsr,

    /// <summary>RAIL - Runway Alignment Indicator Lights.</summary>
    Rail,

    /// <summary>SALS - Short Approach Lighting System.</summary>
    Sals,

    /// <summary>SALSF - Short Approach Lighting System with Sequence Flashing Lights.</summary>
    Salsf,

    /// <summary>SSALS - Simplified Short Approach Lighting System.</summary>
    Ssals,

    /// <summary>SSALF - Simplified Short Approach Lighting System with Sequenced Flashers.</summary>
    Ssalf,

    /// <summary>SSALR - Simplified Short Approach Lighting System with Runway Alignment Indicator Lights.</summary>
    Ssalr,

    /// <summary>ODALS - Omnidirectional Approach Lighting System.</summary>
    Odals,

    /// <summary>RLLS - Runway Lead-In Light System.</summary>
    Rlls,

    /// <summary>MIL OVRN - Military Overrun.</summary>
    MilitaryOverrun,

    /// <summary>NSTD - All other non-standard approach lighting systems.</summary>
    NonStandard
}

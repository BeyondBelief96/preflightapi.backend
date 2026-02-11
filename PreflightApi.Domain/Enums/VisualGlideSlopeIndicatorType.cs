using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Visual Glide Slope Indicators. Corresponds to FAA NASR field VGSI_CODE (APT_RWY_END).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VisualGlideSlopeIndicatorType
{
    /// <summary>VGSI type could not be determined from FAA data.</summary>
    Unknown,

    /// <summary>NONE/N - No approach slope light system.</summary>
    None,

    /// <summary>S2L - 2-Box SAVASI (Simplified Abbreviated Visual Approach Slope Indicator) on left side of runway.</summary>
    Savasi2BoxLeft,

    /// <summary>S2R - 2-Box SAVASI (Simplified Abbreviated Visual Approach Slope Indicator) on right side of runway.</summary>
    Savasi2BoxRight,

    /// <summary>V2L - 2-Box VASI (Visual Approach Slope Indicator) on left side of runway.</summary>
    Vasi2BoxLeft,

    /// <summary>V2R - 2-Box VASI (Visual Approach Slope Indicator) on right side of runway.</summary>
    Vasi2BoxRight,

    /// <summary>V4L - 4-Box VASI (Visual Approach Slope Indicator) on left side of runway.</summary>
    Vasi4BoxLeft,

    /// <summary>V4R - 4-Box VASI (Visual Approach Slope Indicator) on right side of runway.</summary>
    Vasi4BoxRight,

    /// <summary>V6L - 6-Box VASI (Visual Approach Slope Indicator) on left side of runway.</summary>
    Vasi6BoxLeft,

    /// <summary>V6R - 6-Box VASI (Visual Approach Slope Indicator) on right side of runway.</summary>
    Vasi6BoxRight,

    /// <summary>V12 - 12-Box VASI (Visual Approach Slope Indicator) on both sides of runway.</summary>
    Vasi12Box,

    /// <summary>V16 - 16-Box VASI (Visual Approach Slope Indicator) on both sides of runway.</summary>
    Vasi16Box,

    /// <summary>P2L - 2-Light PAPI (Precision Approach Path Indicator) on left side of runway.</summary>
    Papi2LightLeft,

    /// <summary>P2R - 2-Light PAPI (Precision Approach Path Indicator) on right side of runway.</summary>
    Papi2LightRight,

    /// <summary>P4L - 4-Light PAPI (Precision Approach Path Indicator) on left side of runway.</summary>
    Papi4LightLeft,

    /// <summary>P4R - 4-Light PAPI (Precision Approach Path Indicator) on right side of runway.</summary>
    Papi4LightRight,

    /// <summary>TRIL - Tri-Color VASI on left side of runway.</summary>
    TriColorLeft,

    /// <summary>TRIR - Tri-Color VASI on right side of runway.</summary>
    TriColorRight,

    /// <summary>PSIL - Pulsating/Steady Burning VASI on left side of runway.</summary>
    PulsatingLeft,

    /// <summary>PSIR - Pulsating/Steady Burning VASI on right side of runway.</summary>
    PulsatingRight,

    /// <summary>PNIL - System of panels on left side of runway that may or may not be lighted.</summary>
    PanelLeft,

    /// <summary>PNIR - System of panels on right side of runway that may or may not be lighted.</summary>
    PanelRight,

    /// <summary>NSTD - Nonstandard VASI system.</summary>
    NonStandard,

    /// <summary>PVT - Privately owned approach slope indicator light system on a public use airport that is intended for private use only.</summary>
    PrivateUse,

    /// <summary>VAS - Non-specific VASI system.</summary>
    NonSpecificVasi
}

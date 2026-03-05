using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Instrument Landing System (ILS) Type. Corresponds to FAA NASR field ILS_TYPE (APT_RWY_END).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InstrumentApproachType
{
    /// <summary>No instrument approach available.</summary>
    None,

    /// <summary>ILS - Instrument Landing System.</summary>
    Ils,

    /// <summary>MLS - Microwave Landing System.</summary>
    Mls,

    /// <summary>SDF - Simplified Directional Facility.</summary>
    Sdf,

    /// <summary>LOCALIZER - Localizer.</summary>
    Localizer,

    /// <summary>LDA - Localizer-Type Directional Aid.</summary>
    Lda,

    /// <summary>ISMLS - Interim Standard Microwave Landing System.</summary>
    Ismls,

    /// <summary>ILS/DME - Instrument Landing System/Distance Measuring Equipment.</summary>
    IlsDme,

    /// <summary>SDF/DME - Simplified Directional Facility/Distance Measuring Equipment.</summary>
    SdfDme,

    /// <summary>LOC/DME - Localizer/Distance Measuring Equipment.</summary>
    LocDme,

    /// <summary>LOC/GS - Localizer/Glide Slope.</summary>
    LocGs,

    /// <summary>LDA/DME - Localizer-Type Directional Aid/Distance Measuring Equipment.</summary>
    LdaDme
}

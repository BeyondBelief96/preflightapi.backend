using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// NAVAID facility type. Corresponds to FAA NASR field NAV_TYPE (NAV1).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NavaidType
{
    /// <summary>Facility type could not be determined from FAA data.</summary>
    Unknown,

    /// <summary>CONSOLAN - Consolan facility.</summary>
    Consolan,

    /// <summary>DME - Distance Measuring Equipment.</summary>
    Dme,

    /// <summary>FAN MARKER - Fan Marker.</summary>
    FanMarker,

    /// <summary>MARINE NDB - Marine Non-Directional Beacon.</summary>
    MarineNdb,

    /// <summary>MARINE NDB/DME - Marine Non-Directional Beacon with DME.</summary>
    MarineNdbDme,

    /// <summary>NDB - Non-Directional Beacon.</summary>
    Ndb,

    /// <summary>NDB/DME - Non-Directional Beacon with DME.</summary>
    NdbDme,

    /// <summary>TACAN - Tactical Air Navigation.</summary>
    Tacan,

    /// <summary>UHF/NDB - Ultra High Frequency NDB.</summary>
    UhfNdb,

    /// <summary>VOR - VHF Omnidirectional Range.</summary>
    Vor,

    /// <summary>VORTAC - VOR co-located with TACAN.</summary>
    Vortac,

    /// <summary>VOR/DME - VOR co-located with DME.</summary>
    VorDme,

    /// <summary>VOT - VOR Test Facility.</summary>
    Vot
}

using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Landing Facility Site Type. Corresponds to FAA NASR field SITE_TYPE_CODE (APT_BASE).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AirportSiteType
{
    /// <summary>A - Airport.</summary>
    Airport,

    /// <summary>H - Heliport.</summary>
    Heliport,

    /// <summary>C - Seaplane Base.</summary>
    SeaplaneBase,

    /// <summary>B - Balloonport.</summary>
    Balloonport,

    /// <summary>G - Gliderport.</summary>
    Gliderport,

    /// <summary>U - Ultralight.</summary>
    Ultralight
}

using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Landing Facility Site Type. Corresponds to FAA NASR field SITE_TYPE_CODE (APT_BASE).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AirportSiteType
{
    /// <summary>Site type could not be determined from FAA data.</summary>
    Unknown,

    /// <summary>A - Airport.</summary>
    Airport,

    /// <summary>H - Heliport.</summary>
    Heliport,

    /// <summary>S - Seaplane Base.</summary>
    SeaplaneBase,

    /// <summary>G - Gliderport.</summary>
    Gliderport,

    /// <summary>U - Ultralight.</summary>
    Ultralight
}

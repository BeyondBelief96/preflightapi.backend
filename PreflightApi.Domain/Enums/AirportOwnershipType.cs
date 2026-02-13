using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Airport Ownership Type. Corresponds to FAA NASR field OWNERSHIP_TYPE_CODE (APT_BASE).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AirportOwnershipType
{
    /// <summary>Ownership type could not be determined from FAA data.</summary>
    Unknown,

    /// <summary>PU - Publicly Owned.</summary>
    PubliclyOwned,

    /// <summary>PR - Privately Owned.</summary>
    PrivatelyOwned,

    /// <summary>MA - Air Force Owned.</summary>
    AirForce,

    /// <summary>MN - Navy Owned.</summary>
    Navy,

    /// <summary>MR - Army Owned.</summary>
    Army
}

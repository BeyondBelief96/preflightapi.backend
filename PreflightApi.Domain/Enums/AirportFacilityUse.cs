using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Airport Facility Use. Corresponds to FAA NASR field FACILITY_USE_CODE (APT_BASE).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AirportFacilityUse
{
    /// <summary>Facility use could not be determined from FAA data.</summary>
    Unknown,

    /// <summary>PU - Open to the public.</summary>
    PublicUse,

    /// <summary>PR - Private use only.</summary>
    PrivateUse
}

using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Pavement strength determination method. Corresponds to FAA NASR field DTRM_METHOD_CODE (APT_RWY).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PavementDeterminationMethod
{
    /// <summary>T - Technical evaluation.</summary>
    Technical,

    /// <summary>U - Using aircraft (based on experience with aircraft actually using the pavement).</summary>
    UsingAircraft
}

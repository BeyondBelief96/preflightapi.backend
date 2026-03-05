using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Airport Inspection Method. Corresponds to FAA NASR field INSPECT_METHOD_CODE (APT_BASE).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AirportInspectionMethod
{
    /// <summary>F - Federal.</summary>
    Federal,

    /// <summary>S - State.</summary>
    State,

    /// <summary>C - Contractor.</summary>
    Contractor,

    /// <summary>1 - 5010-1 Public Use Mailout Program.</summary>
    PublicUseMailout,

    /// <summary>2 - 5010-2 Private Use Mailout Program.</summary>
    PrivateUseMailout
}

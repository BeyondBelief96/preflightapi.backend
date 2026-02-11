using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Airport Operational Status. Corresponds to FAA NASR field ARPT_STATUS (APT_BASE).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AirportStatus
{
    /// <summary>Status could not be determined from FAA data.</summary>
    Unknown,

    /// <summary>O - Operational.</summary>
    Operational,

    /// <summary>CI - Closed Indefinitely.</summary>
    ClosedIndefinitely,

    /// <summary>CP - Closed Permanently.</summary>
    ClosedPermanently
}

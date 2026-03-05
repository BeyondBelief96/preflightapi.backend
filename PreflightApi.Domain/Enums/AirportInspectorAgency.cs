using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Airport Inspector Agency. Corresponds to FAA NASR field INSPECTOR_CODE (APT_BASE).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AirportInspectorAgency
{
    /// <summary>F - FAA Airports Field Personnel.</summary>
    Faa,

    /// <summary>S - State Aeronautical Personnel.</summary>
    State,

    /// <summary>C - Private Contract Personnel.</summary>
    Contractor,

    /// <summary>N - Owner.</summary>
    Owner
}

using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Airport Inspector Agency. Corresponds to FAA NASR field INSPECTOR_CODE (APT_BASE).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AirportInspectorAgency
{
    /// <summary>Inspector agency could not be determined from FAA data.</summary>
    Unknown,

    /// <summary>F - FAA.</summary>
    Faa,

    /// <summary>S - State.</summary>
    State,

    /// <summary>C - Contractor.</summary>
    Contractor
}

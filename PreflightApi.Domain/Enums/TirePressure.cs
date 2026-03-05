using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Tire Pressure Category. Corresponds to FAA NASR field TIRE_PRES_CODE (APT_RWY).
/// Part of the Pavement Classification Number (PCN) system.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TirePressure
{
    /// <summary>W - High pressure (no limit).</summary>
    High,

    /// <summary>X - Medium pressure (limited to 217 psi / 1.5 MPa).</summary>
    Medium,

    /// <summary>Y - Low pressure (limited to 145 psi / 1.0 MPa).</summary>
    Low,

    /// <summary>Z - Very low pressure (limited to 73 psi / 0.5 MPa).</summary>
    VeryLow
}

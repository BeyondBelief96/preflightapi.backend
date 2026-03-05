using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Oxygen Pressure Type. Corresponds to FAA NASR fields BOTTLED_OXY_TYPE and BULK_OXY_TYPE (APT_BASE).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OxygenPressureType
{
    /// <summary>NONE - No oxygen available.</summary>
    None,

    /// <summary>HIGH - High pressure oxygen available.</summary>
    High,

    /// <summary>LOW - Low pressure oxygen available.</summary>
    Low,

    /// <summary>HIGH/LOW - Both high and low pressure oxygen available.</summary>
    HighAndLow
}

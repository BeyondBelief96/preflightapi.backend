using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Runway Markings Type. Corresponds to FAA NASR field RWY_MARKING_TYPE_CODE (APT_RWY_END).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RunwayMarkingsType
{
    /// <summary>Markings type could not be determined from FAA data.</summary>
    Unknown,

    /// <summary>NONE - No runway markings.</summary>
    None,

    /// <summary>PIR - Precision Instrument runway markings.</summary>
    PrecisionInstrument,

    /// <summary>NPI - Nonprecision Instrument runway markings.</summary>
    NonPrecisionInstrument,

    /// <summary>BSC - Basic runway markings.</summary>
    Basic,

    /// <summary>NRS - Numbers Only runway markings.</summary>
    NumbersOnly,

    /// <summary>NSTD - Nonstandard runway markings (other than numbers only).</summary>
    NonStandard,

    /// <summary>BUOY - Buoys (seaplane base).</summary>
    Buoys,

    /// <summary>STOL - Short Takeoff and Landing runway markings.</summary>
    Stol
}

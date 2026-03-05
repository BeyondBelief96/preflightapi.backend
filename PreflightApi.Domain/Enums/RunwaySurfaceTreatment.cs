using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Runway Surface Treatment. Corresponds to FAA NASR field TREATMENT_CODE (APT_RWY).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RunwaySurfaceTreatment
{
    /// <summary>NONE - No special surface treatment.</summary>
    None,

    /// <summary>GRVD - Saw-Cut or Plastic Grooved.</summary>
    Grooved,

    /// <summary>PFC - Porous Friction Course.</summary>
    PorousFrictionCourse,

    /// <summary>AFSC - Aggregate Friction Seal Coat.</summary>
    AggregateFrictionSealCoat,

    /// <summary>RFSC - Rubberized Friction Seal Coat.</summary>
    RubberizedFrictionSealCoat,

    /// <summary>WC - Wire Comb or Wire Tine.</summary>
    WireComb
}

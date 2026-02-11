using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Runway Surface Type. Corresponds to FAA NASR field SURFACE_TYPE_CODE (APT_RWY).
/// The value will usually be one of the common types or a combination of two types
/// when the runway is composed of distinct sections.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RunwaySurfaceType
{
    /// <summary>Surface type could not be determined from FAA data.</summary>
    Unknown,

    /// <summary>CONC - Portland Cement Concrete.</summary>
    Concrete,

    /// <summary>ASPH - Asphalt or Bituminous Concrete.</summary>
    Asphalt,

    /// <summary>SNOW - Snow.</summary>
    Snow,

    /// <summary>ICE - Ice.</summary>
    Ice,

    /// <summary>MATS - Pierced Steel Planking (PSP); Landing Mats; Membranes.</summary>
    Mats,

    /// <summary>TREATED/TRTD - Oiled; Soil Cement or Lime Stabilized.</summary>
    Treated,

    /// <summary>GRAVEL - Gravel; Cinders; Crushed Rock; Coral or Shells; Slag.</summary>
    Gravel,

    /// <summary>TURF - Grass; Sod.</summary>
    Turf,

    /// <summary>DIRT - Natural Soil.</summary>
    Dirt,

    /// <summary>PEM - Partially Concrete, Asphalt or Bitumen-Bound Macadam.</summary>
    PartiallyPaved,

    /// <summary>ROOF-TOP - Material Not Specified.</summary>
    Rooftop,

    /// <summary>WATER - Water (seaplane base).</summary>
    Water,

    /// <summary>ALUMINUM - Aluminum surface (less common).</summary>
    Aluminum,

    /// <summary>BRICK - Brick surface (less common).</summary>
    Brick,

    /// <summary>CALICHE - Caite surface (less common).</summary>
    Caliche,

    /// <summary>CORAL - Coral surface (less common).</summary>
    Coral,

    /// <summary>DECK - Deck surface (less common).</summary>
    Deck,

    /// <summary>GRASS - Grass surface (less common).</summary>
    Grass,

    /// <summary>METAL - Metal surface (less common).</summary>
    Metal,

    /// <summary>NSTD - Non-standard surface type (less common).</summary>
    NonStandard,

    /// <summary>OIL&amp;CHIP - Oil and chip surface (less common).</summary>
    OilChip,

    /// <summary>PSP - Pierced Steel Planking (less common).</summary>
    Psp,

    /// <summary>SAND - Sand surface (less common).</summary>
    Sand,

    /// <summary>SOD - Sod surface (less common).</summary>
    Sod,

    /// <summary>STEEL - Steel surface (less common).</summary>
    Steel,

    /// <summary>WOOD - Wood surface (less common).</summary>
    Wood
}

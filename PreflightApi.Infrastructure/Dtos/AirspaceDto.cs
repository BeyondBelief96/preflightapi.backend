namespace PreflightApi.Infrastructure.Dtos;

/// <summary>
/// Controlled airspace data (Class B, C, D, E) sourced from FAA ArcGIS.
/// Geometry boundaries are generalized (~1.1m accuracy) for efficient storage and rendering.
/// Use the GlobalId to cross-reference with navigation log results — the navlog response's
/// AirspaceGlobalIds field contains GlobalIds from this endpoint for airspaces along a planned route.
/// </summary>
public record AirspaceDto
{
    /// <summary>ArcGIS global unique identifier. This is the key used to look up airspaces returned by the navigation log endpoint's AirspaceGlobalIds field.</summary>
    public string? GlobalId { get; init; }
    /// <summary>FAA identifier.</summary>
    public string? Ident { get; init; }
    /// <summary>ICAO identifier.</summary>
    public string? IcaoId { get; init; }
    /// <summary>Airspace name.</summary>
    public string? Name { get; init; }
    /// <summary>Upper altitude limit description.</summary>
    public string? UpperDesc { get; init; }
    /// <summary>Upper altitude limit value. Unit determined by <see cref="UpperUom"/> (FT = feet, FL = flight level). Reference determined by <see cref="UpperCode"/> (MSL or AGL).</summary>
    public double? UpperVal { get; init; }
    /// <summary>Upper altitude unit of measure (e.g., FT, FL).</summary>
    public string? UpperUom { get; init; }
    /// <summary>Upper altitude reference code (e.g., MSL, AGL).</summary>
    public string? UpperCode { get; init; }
    /// <summary>Lower altitude limit description.</summary>
    public string? LowerDesc { get; init; }
    /// <summary>Lower altitude limit value. Unit determined by <see cref="LowerUom"/> (FT = feet, FL = flight level). Reference determined by <see cref="LowerCode"/> (MSL or AGL).</summary>
    public double? LowerVal { get; init; }
    /// <summary>Lower altitude unit of measure (e.g., FT, FL).</summary>
    public string? LowerUom { get; init; }
    /// <summary>Lower altitude reference code (e.g., MSL, AGL).</summary>
    public string? LowerCode { get; init; }
    /// <summary>Airspace type code (e.g., CLASS_B, CLASS_C).</summary>
    public string? TypeCode { get; init; }
    /// <summary>Local airspace type.</summary>
    public string? LocalType { get; init; }
    /// <summary>Airspace class (e.g., B, C, D, E).</summary>
    public string? Class { get; init; }
    /// <summary>Military use code.</summary>
    public string? MilCode { get; init; }
    /// <summary>Communications facility name.</summary>
    public string? CommName { get; init; }
    /// <summary>Level designation (e.g., SURFACE, UPPER).</summary>
    public string? Level { get; init; }
    /// <summary>Sector identifier.</summary>
    public string? Sector { get; init; }
    /// <summary>Whether the airspace is onshore.</summary>
    public string? Onshore { get; init; }
    /// <summary>Exclusion area indicator.</summary>
    public string? Exclusion { get; init; }
    /// <summary>Working hours code.</summary>
    public string? WkhrCode { get; init; }
    /// <summary>Working hours remarks.</summary>
    public string? WkhrRmk { get; init; }
    /// <summary>Daylight saving time indicator.</summary>
    public string? Dst { get; init; }
    /// <summary>GMT offset.</summary>
    public string? GmtOffset { get; init; }
    /// <summary>Controlling agency.</summary>
    public string? ContAgent { get; init; }
    /// <summary>City associated with the airspace.</summary>
    public string? City { get; init; }
    /// <summary>State associated with the airspace.</summary>
    public string? State { get; init; }
    /// <summary>Country code.</summary>
    public string? Country { get; init; }
    /// <summary>Associated aerodrome identifier.</summary>
    public string? AdhpId { get; init; }
    /// <summary>GeoJSON boundary geometry. Generalized with ~1.1m accuracy from source data.</summary>
    public GeoJsonGeometry? Geometry { get; init; }
}

/// <summary>
/// Special use airspace data (restricted, prohibited, warning, MOA, alert) sourced from FAA ArcGIS.
/// Geometry boundaries are generalized (~11m accuracy) for efficient storage and rendering.
/// Use the GlobalId to cross-reference with navigation log results — the navlog response's
/// SpecialUseAirspaceGlobalIds field contains GlobalIds from this endpoint for special use airspaces along a planned route.
/// </summary>
public record SpecialUseAirspaceDto
{
    /// <summary>ArcGIS global unique identifier. This is the key used to look up special use airspaces returned by the navigation log endpoint's SpecialUseAirspaceGlobalIds field.</summary>
    public string? GlobalId { get; init; }
    /// <summary>Airspace name.</summary>
    public string? Name { get; init; }
    /// <summary>Type code (e.g., R for restricted, P for prohibited).</summary>
    public string? TypeCode { get; init; }
    /// <summary>Airspace class.</summary>
    public string? Class { get; init; }
    /// <summary>Upper altitude limit description.</summary>
    public string? UpperDesc { get; init; }
    /// <summary>Upper altitude limit value. Unit determined by <see cref="UpperUom"/> (FT = feet, FL = flight level). Reference determined by <see cref="UpperCode"/> (MSL or AGL).</summary>
    public double? UpperVal { get; init; }
    /// <summary>Upper altitude unit of measure (e.g., FT, FL).</summary>
    public string? UpperUom { get; init; }
    /// <summary>Upper altitude reference code (e.g., MSL, AGL).</summary>
    public string? UpperCode { get; init; }
    /// <summary>Lower altitude limit description.</summary>
    public string? LowerDesc { get; init; }
    /// <summary>Lower altitude limit value. Unit determined by <see cref="LowerUom"/> (FT = feet, FL = flight level). Reference determined by <see cref="LowerCode"/> (MSL or AGL).</summary>
    public double? LowerVal { get; init; }
    /// <summary>Lower altitude unit of measure (e.g., FT, FL).</summary>
    public string? LowerUom { get; init; }
    /// <summary>Lower altitude reference code (e.g., MSL, AGL).</summary>
    public string? LowerCode { get; init; }
    /// <summary>Level code.</summary>
    public string? LevelCode { get; init; }
    /// <summary>City associated with the airspace.</summary>
    public string? City { get; init; }
    /// <summary>State associated with the airspace.</summary>
    public string? State { get; init; }
    /// <summary>Country code.</summary>
    public string? Country { get; init; }
    /// <summary>Controlling agency.</summary>
    public string? ContAgent { get; init; }
    /// <summary>Communications facility name.</summary>
    public string? CommName { get; init; }
    /// <summary>Sector identifier.</summary>
    public string? Sector { get; init; }
    /// <summary>Whether the airspace is onshore.</summary>
    public string? Onshore { get; init; }
    /// <summary>Exclusion area indicator.</summary>
    public string? Exclusion { get; init; }
    /// <summary>Times of use for the airspace.</summary>
    public string? TimesOfUse { get; init; }
    /// <summary>GMT offset.</summary>
    public string? GmtOffset { get; init; }
    /// <summary>Daylight saving time code.</summary>
    public string? DstCode { get; init; }
    /// <summary>Additional remarks.</summary>
    public string? Remarks { get; init; }
    /// <summary>GeoJSON boundary geometry. Generalized with ~11m accuracy from source data.</summary>
    public GeoJsonGeometry? Geometry { get; init; }
}

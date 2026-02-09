using PreflightApi.Domain.Constants;

namespace PreflightApi.Infrastructure.Dtos;

/// <summary>
/// Fix/reporting point data from the FAA NASR database (FIX_BASE dataset).
/// Includes waypoints, reporting points, radar fixes, and computer navigation fixes.
/// </summary>
public record FixDto()
{
    /// <summary>Internal unique identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Fix identifier (up to 5 characters, e.g., ACTON, BRAVO).</summary>
    public string FixId { get; init; } = string.Empty;

    /// <summary>ICAO region code (e.g., K1–K7 for CONUS, PA for Alaska).</summary>
    public string IcaoRegionCode { get; init; } = string.Empty;

    /// <summary>Two-letter state/territory code (e.g., TX, CA).</summary>
    public string? StateCode { get; init; }

    /// <summary>Two-letter country code (e.g., US).</summary>
    public string? CountryCode { get; init; }

    /// <summary>FAA 28-day NASR publication effective date.</summary>
    public DateTime EffectiveDate { get; init; }

    /// <summary>Latitude in decimal degrees (positive = north).</summary>
    public decimal? LatDecimal { get; init; }

    /// <summary>Longitude in decimal degrees (negative = west).</summary>
    public decimal? LongDecimal { get; init; }

    /// <summary>Previous fix identifier, if the fix was renamed.</summary>
    public string? FixIdOld { get; init; }

    /// <summary>Charting remark or note associated with the fix.</summary>
    public string? ChartingRemark { get; init; }

    /// <summary>
    /// Fix use/type code indicating the purpose of the fix.
    /// </summary>
    /// <remarks>See <see cref="FixValues.UseCode"/> for known values (e.g., RP, VFR, WP, RADAR, CN, MR, MW, NRS).</remarks>
    public string? FixUseCode { get; init; }

    /// <summary>High-altitude ARTCC identifier responsible for the fix.</summary>
    public string? ArtccIdHigh { get; init; }

    /// <summary>Low-altitude ARTCC identifier responsible for the fix.</summary>
    public string? ArtccIdLow { get; init; }

    /// <summary>Pitch (tilt) flag for directional aid (Y/N).</summary>
    public string? PitchFlag { get; init; }

    /// <summary>Catch flag indicating a catch-type directional aid (Y/N).</summary>
    public string? CatchFlag { get; init; }

    /// <summary>Special Use Airspace / ATC Assigned Airspace flag (Y/N).</summary>
    public string? SuaAtcaaFlag { get; init; }

    /// <summary>Minimum reception altitude in feet MSL.</summary>
    public string? MinReceptionAlt { get; init; }

    /// <summary>
    /// Compulsory reporting point designation. Null indicates a non-compulsory fix.
    /// </summary>
    /// <remarks>See <see cref="FixValues.Compulsory"/> for known values (HIGH, LOW, LOW/HIGH).</remarks>
    public string? Compulsory { get; init; }

    /// <summary>Charts on which the fix appears (semicolon-delimited list).</summary>
    public string? Charts { get; init; }

    /// <summary>Charting type descriptions from the FIX_CHRT supplementary dataset (e.g., IAP, STAR, ENROUTE LOW).</summary>
    public List<string>? ChartingTypes { get; init; }

    /// <summary>NAVAID references defining this fix's position, from the FIX_NAV supplementary dataset.</summary>
    public List<FixNavaidReferenceDto>? NavaidReferences { get; init; }
}

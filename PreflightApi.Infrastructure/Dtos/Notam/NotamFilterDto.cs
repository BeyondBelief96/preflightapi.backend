namespace PreflightApi.Infrastructure.Dtos.Notam;

/// <summary>
/// Optional filters for narrowing NOTAM query results. All filters are combinable — when
/// multiple filters are provided, results must match all of them (AND logic).
/// </summary>
public record NotamFilterDto
{
    /// <summary>
    /// Filter by NOTAM classification.
    /// Valid values: INTERNATIONAL, MILITARY, LOCAL_MILITARY, DOMESTIC, FDC.
    /// </summary>
    public string? Classification { get; init; }

    /// <summary>
    /// Filter by NOTAM feature type (the aeronautical feature the NOTAM applies to).
    /// Valid values: RWY, TWY, APRON, AD, OBST, NAV, COM, SVC, AIRSPACE, ODP, SID, STAR, CHART, DATA, DVA, IAP, VFP, ROUTE, SPECIAL, SECURITY.
    /// </summary>
    public string? Feature { get; init; }

    /// <summary>
    /// Case-insensitive text search within the NOTAM text field (e.g., "CLOSED", "RWY 18/36").
    /// Max 80 characters. Allowed characters: letters, digits, spaces, and /.-().
    /// </summary>
    public string? FreeText { get; init; }

    /// <summary>
    /// Only include NOTAMs with an effective start on or after this date (ISO 8601).
    /// Must be paired with EffectiveEndDate.
    /// </summary>
    public string? EffectiveStartDate { get; init; }

    /// <summary>
    /// Only include NOTAMs with an effective end on or before this date (ISO 8601).
    /// Must be paired with EffectiveStartDate.
    /// </summary>
    public string? EffectiveEndDate { get; init; }

    /// <summary>
    /// Filter by accountability code (issuing office), e.g., "BNA", "FDC", "CLT".
    /// Maps to the account_id column. Alphanumeric, max 10 characters.
    /// </summary>
    public string? Accountability { get; init; }

    /// <summary>
    /// Filter by location identifier (FAA domestic or ICAO code), e.g., "DFW" or "KDFW".
    /// Matches against both the domestic location and ICAO location columns. Alphanumeric, max 10 characters.
    /// </summary>
    public string? Location { get; init; }

    /// <summary>
    /// ISO 8601 timestamp. When provided, returns NOTAMs modified between this time and now,
    /// including both active and inactive NOTAMs (skips the active filter per FAA behavior).
    /// </summary>
    public string? LastUpdatedDate { get; init; }

    /// <summary>
    /// NOTAM number in any supported format. Per FAA spec, must be paired with Location or Accountability.
    /// </summary>
    public string? NotamNumber { get; init; }

    /// <summary>
    /// Latitude in decimal degrees [-90, 90]. Must be paired with Longitude and Radius.
    /// </summary>
    public double? Latitude { get; init; }

    /// <summary>
    /// Longitude in decimal degrees [-180, 180]. Must be paired with Latitude and Radius.
    /// </summary>
    public double? Longitude { get; init; }

    /// <summary>
    /// Search radius in nautical miles [0, 100]. Must be paired with Latitude and Longitude.
    /// </summary>
    public double? Radius { get; init; }

    /// <summary>
    /// Whether any filter values are set.
    /// </summary>
    public bool HasFilters =>
        Classification != null || Feature != null || FreeText != null ||
        EffectiveStartDate != null || EffectiveEndDate != null ||
        Accountability != null || Location != null || LastUpdatedDate != null ||
        NotamNumber != null || Latitude != null || Longitude != null || Radius != null;
}

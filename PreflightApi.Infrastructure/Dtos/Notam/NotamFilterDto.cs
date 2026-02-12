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
    /// Whether any filter values are set.
    /// </summary>
    public bool HasFilters =>
        Classification != null || Feature != null || FreeText != null ||
        EffectiveStartDate != null || EffectiveEndDate != null;
}

namespace PreflightApi.Infrastructure.Dtos.Notam;

/// <summary>
/// Optional NMS query filters for narrowing NOTAM results.
/// </summary>
public record NotamFilterDto
{
    /// <summary>
    /// NOTAM classification filter.
    /// Valid values: INTERNATIONAL, MILITARY, LOCAL_MILITARY, DOMESTIC, FDC
    /// </summary>
    public string? Classification { get; init; }

    /// <summary>
    /// NOTAM feature type filter.
    /// Valid values: RWY, TWY, APRON, AD, OBST, NAV, COM, SVC, AIRSPACE, ODP, SID, STAR, CHART, DATA, DVA, IAP, VFP, ROUTE, SPECIAL, SECURITY
    /// </summary>
    public string? Feature { get; init; }

    /// <summary>
    /// Free text search within NOTAM text. Max 80 characters, pattern: ^[ /.\-\(\)\w]{1,80}$
    /// </summary>
    public string? FreeText { get; init; }

    /// <summary>
    /// Effective start date filter (ISO 8601). Must be paired with EffectiveEndDate.
    /// </summary>
    public string? EffectiveStartDate { get; init; }

    /// <summary>
    /// Effective end date filter (ISO 8601). Must be paired with EffectiveStartDate.
    /// </summary>
    public string? EffectiveEndDate { get; init; }

    /// <summary>
    /// Whether any filter values are set.
    /// </summary>
    public bool HasFilters =>
        Classification != null || Feature != null || FreeText != null ||
        EffectiveStartDate != null || EffectiveEndDate != null;
}

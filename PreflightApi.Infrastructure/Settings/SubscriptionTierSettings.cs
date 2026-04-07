namespace PreflightApi.Infrastructure.Settings;

public class SubscriptionTierSettings
{
    public Dictionary<string, TierDefinition> Tiers { get; set; } = new();
}

public class TierDefinition
{
    /// <summary>Maximum requests per minute for this tier.</summary>
    public int RateLimitPerMinute { get; set; }

    /// <summary>Maximum requests per monthly billing period.</summary>
    public int MonthlyQuota { get; set; }

    /// <summary>
    /// Whitelist of allowed endpoint resource segments (e.g., "metars", "tafs").
    /// If non-empty, only these endpoints are accessible. Empty means all allowed.
    /// </summary>
    public string[] AllowedEndpoints { get; set; } = [];

    /// <summary>
    /// Blocklist of endpoint resource segments (e.g., "navlog", "notams").
    /// These endpoints are denied even if AllowedEndpoints is empty.
    /// </summary>
    public string[] BlockedEndpoints { get; set; } = [];
}

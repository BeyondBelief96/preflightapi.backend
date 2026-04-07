using PreflightApi.Domain.Enums;

namespace PreflightApi.Infrastructure.Dtos;

/// <summary>
/// API key details for listing. The full key is never returned after creation.
/// </summary>
public record ApiKeyDto
{
    /// <summary>First 12 characters of the key for identification (e.g., "pfa_sk_a1b2c").</summary>
    public string Prefix { get; init; } = string.Empty;

    /// <summary>User-chosen label for this key.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Subscription tier determining rate limits, quota, and endpoint access.</summary>
    public SubscriptionTier Tier { get; init; }

    /// <summary>Whether this key is currently active.</summary>
    public bool IsActive { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? LastUsedAt { get; init; }

    public DateTime? RevokedAt { get; init; }

    /// <summary>Number of requests used in the current billing period.</summary>
    public long MonthlyRequestCount { get; init; }

    /// <summary>Maximum requests allowed per billing period for this tier.</summary>
    public int MonthlyQuota { get; init; }

    /// <summary>UTC timestamp when the monthly quota resets.</summary>
    public DateTime QuotaResetAt { get; init; }
}

/// <summary>
/// Request body for creating a new API key.
/// </summary>
public record CreateApiKeyRequestDto
{
    /// <summary>A label to help identify this key (e.g., "Production App", "Dev Testing").</summary>
    public string Name { get; init; } = string.Empty;
}

/// <summary>
/// Response after creating a new API key. The raw key is shown exactly once — store it securely.
/// </summary>
public record CreateApiKeyResponseDto
{
    /// <summary>The full API key. This is the only time it will be shown — store it securely.</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>First 12 characters for future identification.</summary>
    public string Prefix { get; init; } = string.Empty;

    /// <summary>The label you chose for this key.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Subscription tier assigned based on your Stripe subscription.</summary>
    public SubscriptionTier Tier { get; init; }
}

using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

/// <summary>
/// Subscription tier for API access. Determines rate limits, monthly quota, and endpoint access.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SubscriptionTier
{
    /// <summary>Free tier — limited endpoints, 10 req/min, 5k/month.</summary>
    StudentPilot,

    /// <summary>Starter tier — most endpoints, 60 req/min, 150k/month.</summary>
    PrivatePilot,

    /// <summary>Professional tier — full access, 300 req/min, 750k/month.</summary>
    CommercialPilot
}

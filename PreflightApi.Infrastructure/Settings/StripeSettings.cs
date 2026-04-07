namespace PreflightApi.Infrastructure.Settings;

public class StripeSettings
{
    /// <summary>Stripe webhook signing secret for verifying event signatures.</summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>Stripe secret API key for server-side API calls.</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Maps Stripe Price IDs to SubscriptionTier names.
    /// Example: { "price_1T2BvH...": "PrivatePilot", "price_1T2Bwk...": "CommercialPilot" }
    /// </summary>
    public Dictionary<string, string> PriceIdToTier { get; set; } = new();
}

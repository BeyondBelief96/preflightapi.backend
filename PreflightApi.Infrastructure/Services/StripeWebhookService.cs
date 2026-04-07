using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Settings;
using Stripe;

namespace PreflightApi.Infrastructure.Services;

public class StripeWebhookService : IStripeWebhookService
{
    private readonly IApiKeyService _apiKeyService;
    private readonly ILogger<StripeWebhookService> _logger;
    private readonly StripeSettings _stripeSettings;

    public StripeWebhookService(
        IApiKeyService apiKeyService,
        ILogger<StripeWebhookService> logger,
        IOptions<StripeSettings> stripeSettings)
    {
        _apiKeyService = apiKeyService;
        _logger = logger;
        _stripeSettings = stripeSettings.Value;
    }

    public async Task ProcessEventAsync(Event stripeEvent, CancellationToken ct = default)
    {
        _logger.LogInformation("Processing Stripe event {EventType} ({EventId})",
            stripeEvent.Type, stripeEvent.Id);

        switch (stripeEvent.Type)
        {
            // Checkout completed — user just paid for the first time or changed plans
            case EventTypes.CheckoutSessionCompleted:
                await HandleCheckoutCompletedAsync(stripeEvent, ct);
                break;

            // Subscription lifecycle
            case EventTypes.CustomerSubscriptionCreated:
            case EventTypes.CustomerSubscriptionUpdated:
            case EventTypes.CustomerSubscriptionResumed:
                await HandleSubscriptionChangeAsync(stripeEvent, ct);
                break;

            case EventTypes.CustomerSubscriptionDeleted:
            case EventTypes.CustomerSubscriptionPaused:
                await HandleSubscriptionDowngradeAsync(stripeEvent, ct);
                break;

            // Customer deleted — deactivate all keys
            case EventTypes.CustomerDeleted:
                await HandleCustomerDeletedAsync(stripeEvent, ct);
                break;

            // Invoice events
            case EventTypes.InvoicePaymentFailed:
                await HandlePaymentFailedAsync(stripeEvent, ct);
                break;

            case EventTypes.InvoicePaid:
                await HandleInvoicePaidAsync(stripeEvent, ct);
                break;

            default:
                _logger.LogDebug("Ignoring unhandled Stripe event type {EventType}", stripeEvent.Type);
                break;
        }
    }

    private async Task HandleCheckoutCompletedAsync(Event stripeEvent, CancellationToken ct)
    {
        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
        if (session == null)
        {
            _logger.LogWarning("Could not deserialize checkout session from event {EventId}", stripeEvent.Id);
            return;
        }

        var customerId = session.CustomerId;
        if (string.IsNullOrEmpty(customerId))
        {
            _logger.LogWarning("No customer ID in checkout session {SessionId}", session.Id);
            return;
        }

        // The subscription is created by Stripe after checkout — the subscription.created
        // event will handle the tier update. But if we have the subscription ID here,
        // we can do it immediately for faster sync.
        var subscriptionId = session.SubscriptionId;
        if (!string.IsNullOrEmpty(subscriptionId))
        {
            _logger.LogInformation(
                "Checkout completed for customer {CustomerId}, subscription {SubId} — tier sync will happen via subscription.created event",
                customerId, subscriptionId);
        }
        else
        {
            _logger.LogInformation("Checkout completed for customer {CustomerId} (no subscription ID in session)",
                customerId);
        }
    }

    private async Task HandleSubscriptionChangeAsync(Event stripeEvent, CancellationToken ct)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription == null)
        {
            _logger.LogWarning("Could not deserialize subscription from event {EventId}", stripeEvent.Id);
            return;
        }

        var customerId = subscription.CustomerId;
        var subscriptionId = subscription.Id;

        // Get the price ID from the first subscription item
        var priceId = subscription.Items?.Data?.FirstOrDefault()?.Price?.Id;
        if (string.IsNullOrEmpty(priceId))
        {
            _logger.LogWarning("No price ID found in subscription {SubId}", subscriptionId);
            return;
        }

        var tier = ResolveTierFromPriceId(priceId);
        if (tier == null)
        {
            _logger.LogWarning("Unknown price ID {PriceId} in subscription {SubId}", priceId, subscriptionId);
            return;
        }

        // Only update if subscription is active or trialing
        if (subscription.Status is "active" or "trialing")
        {
            await _apiKeyService.UpdateTierByStripeCustomerAsync(customerId, tier.Value, ct);
            _logger.LogInformation("Updated tier to {Tier} for customer {CustomerId} (subscription {SubId})",
                tier.Value, customerId, subscriptionId);
        }
        else
        {
            _logger.LogInformation("Subscription {SubId} status is {Status}, skipping tier update",
                subscriptionId, subscription.Status);
        }
    }

    private async Task HandleSubscriptionDowngradeAsync(Event stripeEvent, CancellationToken ct)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription == null)
        {
            _logger.LogWarning("Could not deserialize subscription from event {EventId}", stripeEvent.Id);
            return;
        }

        // Downgrade to free tier (don't deactivate keys — let them keep using the free tier)
        await _apiKeyService.UpdateTierByStripeCustomerAsync(
            subscription.CustomerId, SubscriptionTier.StudentPilot, ct);

        _logger.LogInformation("Downgraded customer {CustomerId} to StudentPilot ({EventType}, subscription {SubId})",
            subscription.CustomerId, stripeEvent.Type, subscription.Id);
    }

    private async Task HandleCustomerDeletedAsync(Event stripeEvent, CancellationToken ct)
    {
        var customer = stripeEvent.Data.Object as Customer;
        if (customer == null)
        {
            _logger.LogWarning("Could not deserialize customer from event {EventId}", stripeEvent.Id);
            return;
        }

        // Deactivate all API keys for this customer
        await _apiKeyService.DeactivateByStripeCustomerAsync(customer.Id, ct);

        _logger.LogInformation("Deactivated all API keys for deleted customer {CustomerId}", customer.Id);
    }

    private async Task HandlePaymentFailedAsync(Event stripeEvent, CancellationToken ct)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null)
        {
            _logger.LogWarning("Could not deserialize invoice from event {EventId}", stripeEvent.Id);
            return;
        }

        var customerId = invoice.CustomerId;
        if (string.IsNullOrEmpty(customerId))
        {
            _logger.LogWarning("No customer ID in failed invoice {InvoiceId}", invoice.Id);
            return;
        }

        // Immediate downgrade on payment failure
        await _apiKeyService.UpdateTierByStripeCustomerAsync(customerId, SubscriptionTier.StudentPilot, ct);

        _logger.LogWarning("Payment failed for customer {CustomerId} (invoice {InvoiceId}) — downgraded to StudentPilot",
            customerId, invoice.Id);
    }

    private async Task HandleInvoicePaidAsync(Event stripeEvent, CancellationToken ct)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null)
        {
            _logger.LogWarning("Could not deserialize invoice from event {EventId}", stripeEvent.Id);
            return;
        }

        var customerId = invoice.CustomerId;
        if (string.IsNullOrEmpty(customerId))
            return;

        // Reset monthly quota on successful payment (new billing period)
        await _apiKeyService.ResetQuotaByStripeCustomerAsync(customerId, ct);

        _logger.LogInformation("Invoice paid for customer {CustomerId} — quota reset", customerId);
    }

    private SubscriptionTier? ResolveTierFromPriceId(string priceId)
    {
        if (!_stripeSettings.PriceIdToTier.TryGetValue(priceId, out var tierName))
            return null;

        return Enum.TryParse<SubscriptionTier>(tierName, out var tier) ? tier : null;
    }
}

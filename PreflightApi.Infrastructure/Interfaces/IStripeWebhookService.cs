using Stripe;

namespace PreflightApi.Infrastructure.Interfaces;

public interface IStripeWebhookService
{
    Task ProcessEventAsync(Event stripeEvent, CancellationToken ct = default);
}

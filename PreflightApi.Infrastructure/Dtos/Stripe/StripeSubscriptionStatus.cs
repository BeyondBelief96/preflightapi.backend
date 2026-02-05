namespace PreflightApi.Infrastructure.Dtos.Stripe;

public enum StripeSubscriptionStatus
{
    Active,
    Trialing,
    Canceled,
    PastDue,
    Unpaid,
    Paused,
    Incomplete,
    IncompleteExpired
}
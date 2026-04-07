using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;
using Microsoft.Extensions.Options;
using NSubstitute;
using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Services;
using PreflightApi.Infrastructure.Settings;
using Stripe;

namespace PreflightApi.Tests.ApiKeyTests;

public class StripeWebhookServiceTests
{
    private readonly IApiKeyService _apiKeyService;
    private readonly StripeWebhookService _sut;

    private static readonly StripeSettings TestSettings = new()
    {
        PriceIdToTier = new Dictionary<string, string>
        {
            ["price_private"] = "PrivatePilot",
            ["price_commercial"] = "CommercialPilot"
        }
    };

    public StripeWebhookServiceTests()
    {
        _apiKeyService = Substitute.For<IApiKeyService>();
        _sut = new StripeWebhookService(
            _apiKeyService,
            Substitute.For<ILogger<StripeWebhookService>>(),
            Options.Create(TestSettings));
    }

    // ─── Subscription Created / Updated ─────────────────────────────────────

    [Fact]
    public async Task ProcessEvent_SubscriptionCreated_ShouldUpdateTier_WhenActiveWithKnownPrice()
    {
        // Arrange
        var stripeEvent = CreateSubscriptionEvent(
            EventTypes.CustomerSubscriptionCreated, "cus_123", "sub_456", "price_commercial", "active");

        // Act
        await _sut.ProcessEventAsync(stripeEvent);

        // Assert
        await _apiKeyService.Received(1)
            .UpdateTierByStripeCustomerAsync("cus_123", SubscriptionTier.CommercialPilot, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessEvent_SubscriptionUpdated_ShouldUpdateTier_WhenTrialing()
    {
        // Arrange
        var stripeEvent = CreateSubscriptionEvent(
            EventTypes.CustomerSubscriptionUpdated, "cus_123", "sub_456", "price_private", "trialing");

        // Act
        await _sut.ProcessEventAsync(stripeEvent);

        // Assert
        await _apiKeyService.Received(1)
            .UpdateTierByStripeCustomerAsync("cus_123", SubscriptionTier.PrivatePilot, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessEvent_SubscriptionUpdated_ShouldSkip_WhenStatusIsPastDue()
    {
        // Arrange
        var stripeEvent = CreateSubscriptionEvent(
            EventTypes.CustomerSubscriptionUpdated, "cus_123", "sub_456", "price_commercial", "past_due");

        // Act
        await _sut.ProcessEventAsync(stripeEvent);

        // Assert
        await _apiKeyService.DidNotReceive()
            .UpdateTierByStripeCustomerAsync(Arg.Any<string>(), Arg.Any<SubscriptionTier>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessEvent_SubscriptionCreated_ShouldSkip_WhenPriceIdUnknown()
    {
        // Arrange
        var stripeEvent = CreateSubscriptionEvent(
            EventTypes.CustomerSubscriptionCreated, "cus_123", "sub_456", "price_unknown", "active");

        // Act
        await _sut.ProcessEventAsync(stripeEvent);

        // Assert
        await _apiKeyService.DidNotReceive()
            .UpdateTierByStripeCustomerAsync(Arg.Any<string>(), Arg.Any<SubscriptionTier>(), Arg.Any<CancellationToken>());
    }

    // ─── Subscription Deleted / Paused ──────────────────────────────────────

    [Fact]
    public async Task ProcessEvent_SubscriptionDeleted_ShouldDowngradeToStudentPilot()
    {
        // Arrange
        var stripeEvent = CreateSubscriptionEvent(
            EventTypes.CustomerSubscriptionDeleted, "cus_123", "sub_456", "price_commercial", "canceled");

        // Act
        await _sut.ProcessEventAsync(stripeEvent);

        // Assert
        await _apiKeyService.Received(1)
            .UpdateTierByStripeCustomerAsync("cus_123", SubscriptionTier.StudentPilot, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessEvent_SubscriptionPaused_ShouldDowngradeToStudentPilot()
    {
        // Arrange
        var stripeEvent = CreateSubscriptionEvent(
            EventTypes.CustomerSubscriptionPaused, "cus_123", "sub_456", "price_private", "paused");

        // Act
        await _sut.ProcessEventAsync(stripeEvent);

        // Assert
        await _apiKeyService.Received(1)
            .UpdateTierByStripeCustomerAsync("cus_123", SubscriptionTier.StudentPilot, Arg.Any<CancellationToken>());
    }

    // ─── Subscription Resumed ───────────────────────────────────────────────

    [Fact]
    public async Task ProcessEvent_SubscriptionResumed_ShouldRestorePaidTier()
    {
        // Arrange
        var stripeEvent = CreateSubscriptionEvent(
            EventTypes.CustomerSubscriptionResumed, "cus_123", "sub_456", "price_commercial", "active");

        // Act
        await _sut.ProcessEventAsync(stripeEvent);

        // Assert
        await _apiKeyService.Received(1)
            .UpdateTierByStripeCustomerAsync("cus_123", SubscriptionTier.CommercialPilot, Arg.Any<CancellationToken>());
    }

    // ─── Customer Deleted ───────────────────────────────────────────────────

    [Fact]
    public async Task ProcessEvent_CustomerDeleted_ShouldDeactivateAllKeys()
    {
        // Arrange
        var stripeEvent = CreateCustomerDeletedEvent("cus_123");

        // Act
        await _sut.ProcessEventAsync(stripeEvent);

        // Assert
        await _apiKeyService.Received(1)
            .DeactivateByStripeCustomerAsync("cus_123", Arg.Any<CancellationToken>());
    }

    // ─── Invoice Payment Failed ─────────────────────────────────────────────

    [Fact]
    public async Task ProcessEvent_InvoicePaymentFailed_ShouldDowngradeToStudentPilot()
    {
        // Arrange
        var stripeEvent = CreateInvoiceEvent(EventTypes.InvoicePaymentFailed, "cus_123", "inv_789");

        // Act
        await _sut.ProcessEventAsync(stripeEvent);

        // Assert
        await _apiKeyService.Received(1)
            .UpdateTierByStripeCustomerAsync("cus_123", SubscriptionTier.StudentPilot, Arg.Any<CancellationToken>());
    }

    // ─── Invoice Paid ───────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessEvent_InvoicePaid_ShouldResetQuota()
    {
        // Arrange
        var stripeEvent = CreateInvoiceEvent(EventTypes.InvoicePaid, "cus_123", "inv_789");

        // Act
        await _sut.ProcessEventAsync(stripeEvent);

        // Assert
        await _apiKeyService.Received(1)
            .ResetQuotaByStripeCustomerAsync("cus_123", Arg.Any<CancellationToken>());
    }

    // ─── Unhandled Events ───────────────────────────────────────────────────

    [Fact]
    public async Task ProcessEvent_UnknownEventType_ShouldNotCallAnyService()
    {
        // Arrange
        var stripeEvent = new Event { Type = "some.random.event", Id = "evt_test" };

        // Act
        await _sut.ProcessEventAsync(stripeEvent);

        // Assert
        await _apiKeyService.DidNotReceiveWithAnyArgs()
            .UpdateTierByStripeCustomerAsync(default!, default, default);
        await _apiKeyService.DidNotReceiveWithAnyArgs()
            .DeactivateByStripeCustomerAsync(default!, default);
        await _apiKeyService.DidNotReceiveWithAnyArgs()
            .ResetQuotaByStripeCustomerAsync(default!, default);
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private static Event CreateSubscriptionEvent(string eventType, string customerId, string subscriptionId, string priceId, string status)
    {
        var subscription = new Subscription
        {
            Id = subscriptionId,
            CustomerId = customerId,
            Status = status,
            Items = new StripeList<SubscriptionItem>
            {
                Data = [new SubscriptionItem { Price = new Price { Id = priceId } }]
            }
        };

        return new Event
        {
            Id = $"evt_{Guid.NewGuid():N}",
            Type = eventType,
            Data = new EventData { Object = subscription }
        };
    }

    private static Event CreateCustomerDeletedEvent(string customerId)
    {
        return new Event
        {
            Id = $"evt_{Guid.NewGuid():N}",
            Type = EventTypes.CustomerDeleted,
            Data = new EventData { Object = new Customer { Id = customerId } }
        };
    }

    private static Event CreateInvoiceEvent(string eventType, string customerId, string invoiceId)
    {
        return new Event
        {
            Id = $"evt_{Guid.NewGuid():N}",
            Type = eventType,
            Data = new EventData { Object = new Invoice { Id = invoiceId, CustomerId = customerId } }
        };
    }
}

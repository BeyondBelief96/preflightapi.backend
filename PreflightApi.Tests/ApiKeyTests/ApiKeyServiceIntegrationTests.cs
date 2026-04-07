using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Services;
using PreflightApi.Infrastructure.Settings;
using PreflightApi.Tests.IntegrationTests;

namespace PreflightApi.Tests.ApiKeyTests;

[Collection("Integration")]
public class ApiKeyServiceIntegrationTests : PostgreSqlTestBase
{
    private readonly SubscriptionTierSettings _tierSettings = new()
    {
        Tiers = new Dictionary<string, TierDefinition>
        {
            ["StudentPilot"] = new() { RateLimitPerMinute = 10, MonthlyQuota = 5000 },
            ["PrivatePilot"] = new() { RateLimitPerMinute = 60, MonthlyQuota = 150000 },
            ["CommercialPilot"] = new() { RateLimitPerMinute = 300, MonthlyQuota = 750000 }
        }
    };

    private ApiKeyService CreateService()
    {
        return new ApiKeyService(
            DbContext,
            new MemoryCache(new MemoryCacheOptions()),
            Substitute.For<ILogger<ApiKeyService>>(),
            Options.Create(_tierSettings));
    }

    // ─── Full CRUD Lifecycle ────────────────────────────────────────────────

    [Fact]
    public async Task FullLifecycle_CreateValidateRevokeValidate()
    {
        // Arrange
        var service = CreateService();

        // Act — Create
        var created = await service.CreateAsync("user_lifecycle", "Lifecycle Key", SubscriptionTier.PrivatePilot,
            stripeCustomerId: "cus_lifecycle");

        // Assert — Create
        created.Key.Should().StartWith("pfa_sk_");
        created.Tier.Should().Be(SubscriptionTier.PrivatePilot);

        // Act — Validate
        var validated = await service.ValidateAsync(created.Key);
        validated.Should().NotBeNull();
        validated!.OwnerId.Should().Be("user_lifecycle");
        validated.Tier.Should().Be(SubscriptionTier.PrivatePilot);

        // Act — Revoke
        await service.RevokeAsync("user_lifecycle", created.Prefix);

        // Act — Validate after revoke (need fresh service to avoid cache)
        var service2 = CreateService();
        var afterRevoke = await service2.ValidateAsync(created.Key);
        afterRevoke.Should().BeNull();
    }

    // ─── Tier Upgrade/Downgrade via Stripe Customer ─────────────────────────

    [Fact]
    public async Task TierChange_ShouldUpdateAllKeysForCustomer()
    {
        // Arrange
        var service = CreateService();
        await service.CreateAsync("user_tier", "Key 1", SubscriptionTier.StudentPilot,
            stripeCustomerId: "cus_tier_test");
        await service.CreateAsync("user_tier", "Key 2", SubscriptionTier.StudentPilot,
            stripeCustomerId: "cus_tier_test");

        // Act — Upgrade
        await service.UpdateTierByStripeCustomerAsync("cus_tier_test", SubscriptionTier.CommercialPilot);

        // Assert
        var keys = await service.GetByOwnerAsync("user_tier");
        keys.Should().HaveCount(2);
        keys.Should().AllSatisfy(k => k.Tier.Should().Be(SubscriptionTier.CommercialPilot));

        // Act — Downgrade
        await service.UpdateTierByStripeCustomerAsync("cus_tier_test", SubscriptionTier.StudentPilot);

        // Assert
        var downgraded = await service.GetByOwnerAsync("user_tier");
        downgraded.Should().AllSatisfy(k => k.Tier.Should().Be(SubscriptionTier.StudentPilot));
    }

    // ─── Customer Deletion ──────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateByStripeCustomer_ShouldDeactivateAllKeys()
    {
        // Arrange
        var service = CreateService();
        await service.CreateAsync("user_delete", "Key 1", SubscriptionTier.CommercialPilot,
            stripeCustomerId: "cus_delete_test");
        var created2 = await service.CreateAsync("user_delete", "Key 2", SubscriptionTier.CommercialPilot,
            stripeCustomerId: "cus_delete_test");

        // Act
        await service.DeactivateByStripeCustomerAsync("cus_delete_test");

        // Assert — keys are deactivated
        var keys = await service.GetByOwnerAsync("user_delete");
        keys.Should().HaveCount(2);
        keys.Should().AllSatisfy(k =>
        {
            k.IsActive.Should().BeFalse();
            k.RevokedAt.Should().NotBeNull();
        });

        // Assert — validation fails for deactivated keys
        var service2 = CreateService();
        var validated = await service2.ValidateAsync(created2.Key);
        validated.Should().BeNull();
    }

    // ─── Quota Reset ────────────────────────────────────────────────────────

    [Fact]
    public async Task ResetQuota_ShouldResetCountAndSetNextResetDate()
    {
        // Arrange
        var service = CreateService();
        await service.CreateAsync("user_quota", "Key 1", SubscriptionTier.PrivatePilot,
            stripeCustomerId: "cus_quota_test");

        // Simulate usage
        var key = DbContext.ApiKeys.First(k => k.OwnerId == "user_quota");
        key.MonthlyRequestCount = 75000;
        await DbContext.SaveChangesAsync();

        // Act
        await service.ResetQuotaByStripeCustomerAsync("cus_quota_test");

        // Assert
        var service2 = CreateService();
        var keys = await service2.GetByOwnerAsync("user_quota");
        keys.Should().HaveCount(1);
        keys[0].MonthlyRequestCount.Should().Be(0);
        keys[0].QuotaResetAt.Should().BeAfter(DateTime.UtcNow);
    }

    // ─── Multiple Owners Isolation ──────────────────────────────────────────

    [Fact]
    public async Task Operations_ShouldNotCrossOwnerBoundaries()
    {
        // Arrange
        var service = CreateService();
        var key1 = await service.CreateAsync("user_a", "A's Key", SubscriptionTier.PrivatePilot,
            stripeCustomerId: "cus_a");
        await service.CreateAsync("user_b", "B's Key", SubscriptionTier.CommercialPilot,
            stripeCustomerId: "cus_b");

        // Act — revoke user_a's key
        await service.RevokeAsync("user_a", key1.Prefix);

        // Assert — user_b's key is unaffected
        var bKeys = await service.GetByOwnerAsync("user_b");
        bKeys.Should().HaveCount(1);
        bKeys[0].IsActive.Should().BeTrue();

        // Act — attempt to revoke user_b's key as user_a
        Func<Task> act = async () => await service.RevokeAsync("user_a", bKeys[0].Prefix);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ─── Unique Constraints ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateMultipleKeys_ShouldHaveUniquePrefixesAndHashes()
    {
        // Arrange
        var service = CreateService();

        // Act
        var keys = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            var created = await service.CreateAsync("user_unique", $"Key {i}", SubscriptionTier.StudentPilot);
            keys.Add(created.Prefix);
        }

        // Assert
        keys.Should().OnlyHaveUniqueItems();
    }
}

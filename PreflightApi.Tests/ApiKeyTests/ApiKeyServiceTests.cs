using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Services;
using PreflightApi.Infrastructure.Settings;

namespace PreflightApi.Tests.ApiKeyTests;

public class ApiKeyServiceTests : IDisposable
{
    private readonly PreflightApiDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly ApiKeyService _sut;
    private readonly SubscriptionTierSettings _tierSettings;

    public ApiKeyServiceTests()
    {
        var options = new DbContextOptionsBuilder<PreflightApiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PreflightApiDbContext(options);
        _cache = new MemoryCache(new MemoryCacheOptions());

        _tierSettings = new SubscriptionTierSettings
        {
            Tiers = new Dictionary<string, TierDefinition>
            {
                ["StudentPilot"] = new() { RateLimitPerMinute = 10, MonthlyQuota = 5000 },
                ["PrivatePilot"] = new() { RateLimitPerMinute = 60, MonthlyQuota = 150000 },
                ["CommercialPilot"] = new() { RateLimitPerMinute = 300, MonthlyQuota = 750000 }
            }
        };

        _sut = new ApiKeyService(
            _dbContext,
            _cache,
            Substitute.For<ILogger<ApiKeyService>>(),
            Options.Create(_tierSettings));
    }

    public void Dispose()
    {
        _cache.Dispose();
        _dbContext.Dispose();
    }

    // ─── CreateAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ShouldReturnKeyWithCorrectFormat()
    {
        // Act
        var result = await _sut.CreateAsync("user_123", "My Key", SubscriptionTier.StudentPilot);

        // Assert
        result.Key.Should().StartWith("pfa_sk_");
        result.Key.Should().HaveLength(39); // "pfa_sk_" (7) + 32 random chars
        result.Prefix.Should().HaveLength(12);
        result.Prefix.Should().Be(result.Key[..12]);
        result.Name.Should().Be("My Key");
        result.Tier.Should().Be(SubscriptionTier.StudentPilot);
    }

    [Fact]
    public async Task CreateAsync_ShouldStoreHashNotRawKey()
    {
        // Act
        var result = await _sut.CreateAsync("user_123", "Test", SubscriptionTier.PrivatePilot);

        // Assert
        var stored = await _dbContext.ApiKeys.FirstAsync();
        stored.KeyHash.Should().NotBe(result.Key);
        stored.KeyHash.Should().HaveLength(64); // SHA-256 hex
        stored.KeyHash.Should().Be(ApiKeyService.ComputeSha256Hash(result.Key));
    }

    [Fact]
    public async Task CreateAsync_ShouldSetStripeFieldsWhenProvided()
    {
        // Act
        var result = await _sut.CreateAsync("user_123", "Test", SubscriptionTier.CommercialPilot,
            stripeCustomerId: "cus_abc", stripeSubscriptionId: "sub_xyz");

        // Assert
        var stored = await _dbContext.ApiKeys.FirstAsync();
        stored.StripeCustomerId.Should().Be("cus_abc");
        stored.StripeSubscriptionId.Should().Be("sub_xyz");
        stored.Tier.Should().Be(SubscriptionTier.CommercialPilot);
    }

    [Fact]
    public async Task CreateAsync_ShouldSetQuotaResetToNextMonthStart()
    {
        // Act
        await _sut.CreateAsync("user_123", "Test", SubscriptionTier.StudentPilot);

        // Assert
        var stored = await _dbContext.ApiKeys.FirstAsync();
        stored.QuotaResetAt.Day.Should().Be(1);
        stored.QuotaResetAt.Should().BeAfter(DateTime.UtcNow);
        stored.MonthlyRequestCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_ShouldGenerateUniqueKeys()
    {
        // Act
        var key1 = await _sut.CreateAsync("user_123", "Key 1", SubscriptionTier.StudentPilot);
        var key2 = await _sut.CreateAsync("user_123", "Key 2", SubscriptionTier.StudentPilot);

        // Assert
        key1.Key.Should().NotBe(key2.Key);
        key1.Prefix.Should().NotBe(key2.Prefix);
    }

    // ─── ValidateAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_ShouldReturnApiKey_WhenKeyIsValid()
    {
        // Arrange
        var created = await _sut.CreateAsync("user_123", "Test", SubscriptionTier.PrivatePilot);

        // Act
        var result = await _sut.ValidateAsync(created.Key);

        // Assert
        result.Should().NotBeNull();
        result!.OwnerId.Should().Be("user_123");
        result.Tier.Should().Be(SubscriptionTier.PrivatePilot);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnNull_WhenKeyDoesNotExist()
    {
        // Act
        var result = await _sut.ValidateAsync("pfa_sk_nonexistentkey12345678901234");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnNull_WhenKeyFormatIsInvalid()
    {
        // Act & Assert
        (await _sut.ValidateAsync("invalid_key")).Should().BeNull();
        (await _sut.ValidateAsync("")).Should().BeNull();
        (await _sut.ValidateAsync("sk_test_abc")).Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnNull_WhenKeyIsRevoked()
    {
        // Arrange
        var created = await _sut.CreateAsync("user_123", "Test", SubscriptionTier.StudentPilot);
        await _sut.RevokeAsync("user_123", created.Prefix);

        // Act
        var result = await _sut.ValidateAsync(created.Key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnNull_WhenKeyIsExpired()
    {
        // Arrange
        var created = await _sut.CreateAsync("user_123", "Test", SubscriptionTier.StudentPilot);
        var entity = await _dbContext.ApiKeys.FirstAsync();
        entity.ExpiresAt = DateTime.UtcNow.AddHours(-1);
        await _dbContext.SaveChangesAsync();
        _cache.Remove($"apikey:{entity.KeyHash}"); // Clear cache

        // Act
        var result = await _sut.ValidateAsync(created.Key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_ShouldUseCacheOnSecondCall()
    {
        // Arrange
        var created = await _sut.CreateAsync("user_123", "Test", SubscriptionTier.StudentPilot);

        // Act — first call populates cache
        var result1 = await _sut.ValidateAsync(created.Key);
        // Delete from DB to prove cache is used
        _dbContext.ApiKeys.RemoveRange(_dbContext.ApiKeys);
        await _dbContext.SaveChangesAsync();
        var result2 = await _sut.ValidateAsync(created.Key);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result2!.OwnerId.Should().Be("user_123");
    }

    // ─── GetByOwnerAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetByOwnerAsync_ShouldReturnAllKeysForOwner()
    {
        // Arrange
        await _sut.CreateAsync("user_123", "Key 1", SubscriptionTier.StudentPilot);
        await _sut.CreateAsync("user_123", "Key 2", SubscriptionTier.PrivatePilot);
        await _sut.CreateAsync("user_other", "Other Key", SubscriptionTier.StudentPilot);

        // Act
        var result = await _sut.GetByOwnerAsync("user_123");

        // Assert
        result.Should().HaveCount(2);
        result.Select(k => k.Name).Should().Contain("Key 1").And.Contain("Key 2");
    }

    [Fact]
    public async Task GetByOwnerAsync_ShouldIncludeQuotaInfo()
    {
        // Arrange
        await _sut.CreateAsync("user_123", "Key 1", SubscriptionTier.PrivatePilot);

        // Act
        var result = await _sut.GetByOwnerAsync("user_123");

        // Assert
        result.Should().HaveCount(1);
        result[0].MonthlyQuota.Should().Be(150000);
        result[0].Tier.Should().Be(SubscriptionTier.PrivatePilot);
    }

    [Fact]
    public async Task GetByOwnerAsync_ShouldReturnEmpty_WhenNoKeysExist()
    {
        // Act
        var result = await _sut.GetByOwnerAsync("nonexistent_user");

        // Assert
        result.Should().BeEmpty();
    }

    // ─── RevokeAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task RevokeAsync_ShouldDeactivateKey()
    {
        // Arrange
        var created = await _sut.CreateAsync("user_123", "Test", SubscriptionTier.StudentPilot);

        // Act
        await _sut.RevokeAsync("user_123", created.Prefix);

        // Assert
        var stored = await _dbContext.ApiKeys.FirstAsync();
        stored.IsActive.Should().BeFalse();
        stored.RevokedAt.Should().NotBeNull();
        stored.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RevokeAsync_ShouldThrow_WhenKeyNotFoundForOwner()
    {
        // Arrange
        await _sut.CreateAsync("user_123", "Test", SubscriptionTier.StudentPilot);

        // Act
        Func<Task> act = async () => await _sut.RevokeAsync("user_other", "pfa_sk_xxxxx");

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task RevokeAsync_ShouldEvictCache()
    {
        // Arrange
        var created = await _sut.CreateAsync("user_123", "Test", SubscriptionTier.StudentPilot);
        await _sut.ValidateAsync(created.Key); // Populate cache

        // Act
        await _sut.RevokeAsync("user_123", created.Prefix);
        var result = await _sut.ValidateAsync(created.Key);

        // Assert
        result.Should().BeNull();
    }

    // ─── UpdateTierByStripeCustomerAsync ─────────────────────────────────────

    [Fact]
    public async Task UpdateTierByStripeCustomerAsync_ShouldUpdateAllActiveKeys()
    {
        // Arrange
        await _sut.CreateAsync("user_123", "Key 1", SubscriptionTier.StudentPilot,
            stripeCustomerId: "cus_abc");
        await _sut.CreateAsync("user_123", "Key 2", SubscriptionTier.StudentPilot,
            stripeCustomerId: "cus_abc");

        // Act
        await _sut.UpdateTierByStripeCustomerAsync("cus_abc", SubscriptionTier.CommercialPilot);

        // Assert
        var keys = await _dbContext.ApiKeys.ToListAsync();
        keys.Should().AllSatisfy(k => k.Tier.Should().Be(SubscriptionTier.CommercialPilot));
    }

    [Fact]
    public async Task UpdateTierByStripeCustomerAsync_ShouldNotUpdateOtherCustomers()
    {
        // Arrange
        await _sut.CreateAsync("user_1", "Key 1", SubscriptionTier.StudentPilot,
            stripeCustomerId: "cus_abc");
        await _sut.CreateAsync("user_2", "Key 2", SubscriptionTier.StudentPilot,
            stripeCustomerId: "cus_xyz");

        // Act
        await _sut.UpdateTierByStripeCustomerAsync("cus_abc", SubscriptionTier.CommercialPilot);

        // Assert
        var otherKey = await _dbContext.ApiKeys.FirstAsync(k => k.StripeCustomerId == "cus_xyz");
        otherKey.Tier.Should().Be(SubscriptionTier.StudentPilot);
    }

    // ─── DeactivateByStripeCustomerAsync ────────────────────────────────────

    [Fact]
    public async Task DeactivateByStripeCustomerAsync_ShouldDeactivateAllKeys()
    {
        // Arrange
        await _sut.CreateAsync("user_123", "Key 1", SubscriptionTier.PrivatePilot,
            stripeCustomerId: "cus_abc");
        await _sut.CreateAsync("user_123", "Key 2", SubscriptionTier.PrivatePilot,
            stripeCustomerId: "cus_abc");

        // Act
        await _sut.DeactivateByStripeCustomerAsync("cus_abc");

        // Assert
        var keys = await _dbContext.ApiKeys.ToListAsync();
        keys.Should().AllSatisfy(k =>
        {
            k.IsActive.Should().BeFalse();
            k.RevokedAt.Should().NotBeNull();
        });
    }

    // ─── ResetQuotaByStripeCustomerAsync ────────────────────────────────────

    [Fact]
    public async Task ResetQuotaByStripeCustomerAsync_ShouldResetCountAndDate()
    {
        // Arrange
        await _sut.CreateAsync("user_123", "Key 1", SubscriptionTier.PrivatePilot,
            stripeCustomerId: "cus_abc");
        var key = await _dbContext.ApiKeys.FirstAsync();
        key.MonthlyRequestCount = 5000;
        await _dbContext.SaveChangesAsync();

        // Act
        await _sut.ResetQuotaByStripeCustomerAsync("cus_abc");

        // Assert
        var updated = await _dbContext.ApiKeys.FirstAsync();
        updated.MonthlyRequestCount.Should().Be(0);
        updated.QuotaResetAt.Day.Should().Be(1);
        updated.QuotaResetAt.Should().BeAfter(DateTime.UtcNow);
    }

    // ─── ComputeSha256Hash ──────────────────────────────────────────────────

    [Fact]
    public void ComputeSha256Hash_ShouldBeConsistent()
    {
        // Act
        var hash1 = ApiKeyService.ComputeSha256Hash("test_key_value");
        var hash2 = ApiKeyService.ComputeSha256Hash("test_key_value");

        // Assert
        hash1.Should().Be(hash2);
        hash1.Should().HaveLength(64);
        hash1.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void ComputeSha256Hash_ShouldDifferForDifferentInputs()
    {
        // Act
        var hash1 = ApiKeyService.ComputeSha256Hash("key_a");
        var hash2 = ApiKeyService.ComputeSha256Hash("key_b");

        // Assert
        hash1.Should().NotBe(hash2);
    }
}

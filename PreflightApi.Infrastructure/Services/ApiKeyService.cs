using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Settings;

namespace PreflightApi.Infrastructure.Services;

public class ApiKeyService : IApiKeyService
{
    private readonly PreflightApiDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ApiKeyService> _logger;
    private readonly SubscriptionTierSettings _tierSettings;

    private const string Base62Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const int RandomLength = 32;
    private const int PrefixLength = 12;
    private const string KeyPrefix = "pfa_sk_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    public ApiKeyService(
        PreflightApiDbContext context,
        IMemoryCache cache,
        ILogger<ApiKeyService> logger,
        IOptions<SubscriptionTierSettings> tierSettings)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
        _tierSettings = tierSettings.Value;
    }

    public async Task<CreateApiKeyResponseDto> CreateAsync(string ownerId, string name, SubscriptionTier tier,
        string? stripeCustomerId = null, string? stripeSubscriptionId = null, CancellationToken ct = default)
    {
        var rawKey = GenerateRawKey();
        var prefix = rawKey[..PrefixLength];
        var keyHash = ComputeSha256Hash(rawKey);

        var entity = new ApiKey
        {
            OwnerId = ownerId,
            StripeCustomerId = stripeCustomerId,
            StripeSubscriptionId = stripeSubscriptionId,
            Prefix = prefix,
            KeyHash = keyHash,
            Name = name,
            Tier = tier,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            MonthlyRequestCount = 0,
            QuotaResetAt = GetNextMonthStart()
        };

        _context.ApiKeys.Add(entity);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Created API key {Prefix} for owner {OwnerId} with tier {Tier}",
            prefix, ownerId, tier);

        return new CreateApiKeyResponseDto
        {
            Key = rawKey,
            Prefix = prefix,
            Name = name,
            Tier = tier
        };
    }

    public async Task<ApiKey?> ValidateAsync(string rawKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawKey) || !rawKey.StartsWith(KeyPrefix))
            return null;

        var keyHash = ComputeSha256Hash(rawKey);
        var cacheKey = $"apikey:{keyHash}";

        if (_cache.TryGetValue(cacheKey, out ApiKey? cached))
            return cached;

        var apiKey = await _context.ApiKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash, ct);

        if (apiKey == null)
            return null;

        if (!apiKey.IsActive || apiKey.RevokedAt != null)
        {
            _cache.Set(cacheKey, (ApiKey?)null, CacheDuration);
            return null;
        }

        if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow)
        {
            _cache.Set(cacheKey, (ApiKey?)null, CacheDuration);
            return null;
        }

        _cache.Set(cacheKey, apiKey, CacheDuration);
        return apiKey;
    }

    public async Task<IReadOnlyList<ApiKeyDto>> GetByOwnerAsync(string ownerId, CancellationToken ct = default)
    {
        var keys = await _context.ApiKeys
            .AsNoTracking()
            .Where(k => k.OwnerId == ownerId)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(ct);

        return keys.Select(k =>
        {
            var tierName = k.Tier.ToString();
            var monthlyQuota = _tierSettings.Tiers.TryGetValue(tierName, out var def)
                ? def.MonthlyQuota
                : 0;

            return new ApiKeyDto
            {
                Prefix = k.Prefix,
                Name = k.Name,
                Tier = k.Tier,
                IsActive = k.IsActive,
                CreatedAt = k.CreatedAt,
                LastUsedAt = k.LastUsedAt,
                RevokedAt = k.RevokedAt,
                MonthlyRequestCount = k.MonthlyRequestCount,
                MonthlyQuota = monthlyQuota,
                QuotaResetAt = k.QuotaResetAt
            };
        }).ToList();
    }

    public async Task RevokeAsync(string ownerId, string prefix, CancellationToken ct = default)
    {
        var apiKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.OwnerId == ownerId && k.Prefix == prefix, ct);

        if (apiKey == null)
            throw new KeyNotFoundException($"API key with prefix '{prefix}' not found for this user.");

        apiKey.IsActive = false;
        apiKey.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        // Evict from cache
        _cache.Remove($"apikey:{apiKey.KeyHash}");

        _logger.LogInformation("Revoked API key {Prefix} for owner {OwnerId}", prefix, ownerId);
    }

    public async Task UpdateTierByStripeSubscriptionAsync(string stripeSubscriptionId, SubscriptionTier tier, CancellationToken ct = default)
    {
        var keys = await _context.ApiKeys
            .Where(k => k.StripeSubscriptionId == stripeSubscriptionId && k.IsActive)
            .ToListAsync(ct);

        foreach (var key in keys)
        {
            key.Tier = tier;
            _cache.Remove($"apikey:{key.KeyHash}");
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Updated {Count} keys for subscription {SubId} to tier {Tier}",
            keys.Count, stripeSubscriptionId, tier);
    }

    public async Task UpdateTierByStripeCustomerAsync(string stripeCustomerId, SubscriptionTier tier, CancellationToken ct = default)
    {
        var keys = await _context.ApiKeys
            .Where(k => k.StripeCustomerId == stripeCustomerId && k.IsActive)
            .ToListAsync(ct);

        foreach (var key in keys)
        {
            key.Tier = tier;
            _cache.Remove($"apikey:{key.KeyHash}");
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Updated {Count} keys for customer {CustomerId} to tier {Tier}",
            keys.Count, stripeCustomerId, tier);
    }

    public async Task DeactivateByStripeSubscriptionAsync(string stripeSubscriptionId, CancellationToken ct = default)
    {
        var keys = await _context.ApiKeys
            .Where(k => k.StripeSubscriptionId == stripeSubscriptionId && k.IsActive)
            .ToListAsync(ct);

        foreach (var key in keys)
        {
            key.Tier = SubscriptionTier.StudentPilot;
            _cache.Remove($"apikey:{key.KeyHash}");
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Downgraded {Count} keys for canceled subscription {SubId} to StudentPilot",
            keys.Count, stripeSubscriptionId);
    }

    public async Task DeactivateByStripeCustomerAsync(string stripeCustomerId, CancellationToken ct = default)
    {
        var keys = await _context.ApiKeys
            .Where(k => k.StripeCustomerId == stripeCustomerId && k.IsActive)
            .ToListAsync(ct);

        foreach (var key in keys)
        {
            key.IsActive = false;
            key.RevokedAt = DateTime.UtcNow;
            _cache.Remove($"apikey:{key.KeyHash}");
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Deactivated {Count} keys for deleted customer {CustomerId}",
            keys.Count, stripeCustomerId);
    }

    public async Task ResetQuotaByStripeCustomerAsync(string stripeCustomerId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var nextReset = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);

        var keys = await _context.ApiKeys
            .Where(k => k.StripeCustomerId == stripeCustomerId && k.IsActive)
            .ToListAsync(ct);

        foreach (var key in keys)
        {
            key.MonthlyRequestCount = 0;
            key.QuotaResetAt = nextReset;
            _cache.Remove($"apikey:{key.KeyHash}");
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Reset quota for {Count} keys for customer {CustomerId}",
            keys.Count, stripeCustomerId);
    }

    public async Task UpdateLastUsedAsync(Guid apiKeyId, CancellationToken ct = default)
    {
        await _context.ApiKeys
            .Where(k => k.Id == apiKeyId)
            .ExecuteUpdateAsync(s => s.SetProperty(k => k.LastUsedAt, DateTime.UtcNow), ct);
    }

    private static string GenerateRawKey()
    {
        Span<byte> bytes = stackalloc byte[RandomLength];
        RandomNumberGenerator.Fill(bytes);

        var sb = new StringBuilder(KeyPrefix, KeyPrefix.Length + RandomLength);
        for (int i = 0; i < RandomLength; i++)
            sb.Append(Base62Chars[bytes[i] % Base62Chars.Length]);

        return sb.ToString();
    }

    internal static string ComputeSha256Hash(string input)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static DateTime GetNextMonthStart()
    {
        var now = DateTime.UtcNow;
        return new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
    }
}

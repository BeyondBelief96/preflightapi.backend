using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Dtos;

namespace PreflightApi.Infrastructure.Interfaces;

public interface IApiKeyService
{
    Task<CreateApiKeyResponseDto> CreateAsync(string ownerId, string name, SubscriptionTier tier,
        string? stripeCustomerId = null, string? stripeSubscriptionId = null, CancellationToken ct = default);

    Task<ApiKey?> ValidateAsync(string rawKey, CancellationToken ct = default);

    Task<IReadOnlyList<ApiKeyDto>> GetByOwnerAsync(string ownerId, CancellationToken ct = default);

    Task RevokeAsync(string ownerId, string prefix, CancellationToken ct = default);

    Task UpdateTierByStripeSubscriptionAsync(string stripeSubscriptionId, SubscriptionTier tier, CancellationToken ct = default);

    Task UpdateTierByStripeCustomerAsync(string stripeCustomerId, SubscriptionTier tier, CancellationToken ct = default);

    Task DeactivateByStripeSubscriptionAsync(string stripeSubscriptionId, CancellationToken ct = default);

    Task DeactivateByStripeCustomerAsync(string stripeCustomerId, CancellationToken ct = default);

    Task ResetQuotaByStripeCustomerAsync(string stripeCustomerId, CancellationToken ct = default);

    Task UpdateLastUsedAsync(Guid apiKeyId, CancellationToken ct = default);
}

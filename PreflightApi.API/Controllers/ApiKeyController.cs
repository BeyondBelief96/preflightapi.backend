using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PreflightApi.API.Models;
using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Settings;
using Stripe;

namespace PreflightApi.API.Controllers;

/// <summary>
/// Manage your API keys. Requires Clerk JWT authentication.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/api-keys")]
[Tags("API Key Management")]
[Authorize(AuthenticationSchemes = "ClerkJwt")]
public class ApiKeyController(
    IApiKeyService apiKeyService,
    IOptions<StripeSettings> stripeSettings,
    ILogger<ApiKeyController> logger) : ControllerBase
{
    /// <summary>
    /// Create a new API key. The full key is returned exactly once — store it securely.
    /// Your subscription tier is automatically determined from your Stripe subscription.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateApiKeyResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateApiKeyResponseDto>> CreateApiKey(
        [FromBody] CreateApiKeyRequestDto request,
        CancellationToken ct)
    {
        var ownerId = GetClerkUserId();
        if (ownerId == null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new ApiErrorResponse
            {
                Code = "VALIDATION_ERROR",
                Message = "API key name is required.",
                Timestamp = DateTime.UtcNow.ToString("o")
            });

        // Determine tier from Stripe subscription
        var (tier, stripeCustomerId, stripeSubscriptionId) =
            await ResolveSubscriptionTierAsync(ownerId, request.StripeCustomerId, ct);

        var result = await apiKeyService.CreateAsync(
            ownerId, request.Name, tier, stripeCustomerId, stripeSubscriptionId, ct);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// List all API keys for the authenticated user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ApiKeyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ApiKeyDto>>> ListApiKeys(CancellationToken ct)
    {
        var ownerId = GetClerkUserId();
        if (ownerId == null)
            return Unauthorized();

        var keys = await apiKeyService.GetByOwnerAsync(ownerId, ct);
        return Ok(keys);
    }

    /// <summary>
    /// Revoke an API key by its prefix. This action cannot be undone.
    /// </summary>
    [HttpDelete("{prefix}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeApiKey(string prefix, CancellationToken ct)
    {
        var ownerId = GetClerkUserId();
        if (ownerId == null)
            return Unauthorized();

        try
        {
            await apiKeyService.RevokeAsync(ownerId, prefix, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ApiErrorResponse
            {
                Code = "NOT_FOUND",
                Message = $"API key with prefix '{prefix}' not found.",
                Timestamp = DateTime.UtcNow.ToString("o")
            });
        }
    }

    private string? GetClerkUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
    }

    private async Task<(SubscriptionTier tier, string? stripeCustomerId, string? stripeSubscriptionId)>
        ResolveSubscriptionTierAsync(string clerkUserId, string? stripeCustomerId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(stripeCustomerId))
        {
            logger.LogInformation("No Stripe customer ID provided for {UserId}; defaulting to StudentPilot", clerkUserId);
            return (SubscriptionTier.StudentPilot, null, null);
        }

        try
        {
            var subscriptionService = new SubscriptionService();
            var subscriptions = await subscriptionService.ListAsync(
                new SubscriptionListOptions
                {
                    Customer = stripeCustomerId,
                    Status = "active",
                    Limit = 1
                },
                cancellationToken: ct);

            var activeSubscription = subscriptions.Data.FirstOrDefault();
            if (activeSubscription == null)
            {
                logger.LogInformation("No active Stripe subscription for customer {CustomerId}; defaulting to StudentPilot",
                    stripeCustomerId);
                return (SubscriptionTier.StudentPilot, stripeCustomerId, null);
            }

            var priceId = activeSubscription.Items?.Data?.FirstOrDefault()?.Price?.Id;
            if (string.IsNullOrEmpty(priceId)
                || !stripeSettings.Value.PriceIdToTier.TryGetValue(priceId, out var tierName)
                || !Enum.TryParse<SubscriptionTier>(tierName, out var tier))
            {
                logger.LogWarning("Unrecognized price {PriceId} on subscription {SubId}; defaulting to StudentPilot",
                    priceId, activeSubscription.Id);
                return (SubscriptionTier.StudentPilot, stripeCustomerId, activeSubscription.Id);
            }

            return (tier, stripeCustomerId, activeSubscription.Id);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Stripe lookup failed for customer {CustomerId}; defaulting to StudentPilot",
                stripeCustomerId);
            return (SubscriptionTier.StudentPilot, stripeCustomerId, null);
        }
    }
}

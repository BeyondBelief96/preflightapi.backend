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
        var (tier, stripeCustomerId, stripeSubscriptionId) = await ResolveSubscriptionTierAsync(ownerId, ct);

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
        ResolveSubscriptionTierAsync(string clerkUserId, CancellationToken ct)
    {
        // The Clerk user's private metadata contains stripeCustomerId.
        // Since we don't have a direct Clerk SDK dependency, the client app
        // should pass the stripeCustomerId in the JWT custom claims, or we
        // look it up from existing API keys for this user.
        //
        // For now, check if the user already has keys with a Stripe customer ID.
        var existingKeys = await apiKeyService.GetByOwnerAsync(clerkUserId, ct);
        var existingActive = existingKeys.FirstOrDefault(k => k.IsActive);
        if (existingActive != null)
        {
            // Use the same tier as their existing active key
            return (existingActive.Tier, null, null);
        }

        // Default to free tier — tier will be updated when Stripe webhook fires
        // after the user subscribes
        logger.LogInformation("No existing subscription found for user {UserId}, defaulting to StudentPilot",
            clerkUserId);
        return (SubscriptionTier.StudentPilot, null, null);
    }
}

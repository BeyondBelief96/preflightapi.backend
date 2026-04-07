using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using PreflightApi.API.Models;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Settings;

namespace PreflightApi.API.Middleware;

/// <summary>
/// Checks the authenticated API key's tier against endpoint access rules.
/// Returns 403 TIER_RESTRICTED if the endpoint is not available for the user's tier.
/// </summary>
public class TierGatingMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    // Matches /api/v{N}/ prefix and captures the resource segment after it
    private static readonly Regex ResourceSegmentPattern = new(
        @"^/api/v\d+/([a-z0-9\-]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public TierGatingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IOptions<SubscriptionTierSettings> tierSettings)
    {
        var apiKey = context.Items["ApiKey"] as ApiKey;
        if (apiKey == null)
        {
            // No API key set — either exempt path or dev bypass. Let it through.
            await _next(context);
            return;
        }

        var settings = tierSettings.Value;
        var tierName = apiKey.Tier.ToString();

        if (!settings.Tiers.TryGetValue(tierName, out var tierDef))
        {
            // Unknown tier — allow through (shouldn't happen with enum validation)
            await _next(context);
            return;
        }

        var resourceSegment = ExtractResourceSegment(context.Request.Path);
        if (string.IsNullOrEmpty(resourceSegment))
        {
            await _next(context);
            return;
        }

        // Whitelist mode: if AllowedEndpoints is set, only those are accessible
        if (tierDef.AllowedEndpoints.Length > 0)
        {
            if (!tierDef.AllowedEndpoints.Contains(resourceSegment, StringComparer.OrdinalIgnoreCase))
            {
                await WriteTierRestrictedResponse(context, tierName);
                return;
            }
        }

        // Blocklist mode: if BlockedEndpoints is set, those are denied
        if (tierDef.BlockedEndpoints.Length > 0)
        {
            if (tierDef.BlockedEndpoints.Contains(resourceSegment, StringComparer.OrdinalIgnoreCase))
            {
                await WriteTierRestrictedResponse(context, tierName);
                return;
            }
        }

        await _next(context);
    }

    private static string? ExtractResourceSegment(PathString path)
    {
        if (!path.HasValue) return null;
        var match = ResourceSegmentPattern.Match(path.Value);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static async Task WriteTierRestrictedResponse(HttpContext context, string tierName)
    {
        var tierDisplayNames = new Dictionary<string, string>
        {
            ["StudentPilot"] = "Free",
            ["PrivatePilot"] = "Starter",
            ["CommercialPilot"] = "Professional"
        };

        var displayName = tierDisplayNames.GetValueOrDefault(tierName, tierName);

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        var errorResponse = new ApiErrorResponse
        {
            Code = ErrorCodes.TierRestricted,
            Message = $"This endpoint is not available on the {displayName} tier. Please upgrade your subscription.",
            Timestamp = DateTime.UtcNow.ToString("o"),
            TraceId = context.TraceIdentifier,
            Path = context.Request.Path.Value
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, JsonOptions));
    }
}

public static class TierGatingMiddlewareExtensions
{
    public static IApplicationBuilder UseTierGating(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TierGatingMiddleware>();
    }
}

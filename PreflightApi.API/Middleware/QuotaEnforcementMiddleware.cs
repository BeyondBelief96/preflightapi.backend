using System.Text.Json;
using Microsoft.Extensions.Options;
using PreflightApi.API.Models;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Settings;

namespace PreflightApi.API.Middleware;

/// <summary>
/// Enforces monthly request quotas per API key. Increments the in-memory counter
/// and returns 429 QUOTA_EXCEEDED if the limit is reached.
/// </summary>
public class QuotaEnforcementMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public QuotaEnforcementMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IQuotaTrackingService quotaTrackingService,
        IOptions<SubscriptionTierSettings> tierSettings)
    {
        var apiKey = context.Items["ApiKey"] as ApiKey;
        if (apiKey == null)
        {
            await _next(context);
            return;
        }

        var settings = tierSettings.Value;
        var tierName = apiKey.Tier.ToString();

        if (!settings.Tiers.TryGetValue(tierName, out var tierDef))
        {
            await _next(context);
            return;
        }

        var (allowed, currentCount) = quotaTrackingService.IncrementAndCheck(
            apiKey.Id, apiKey.MonthlyRequestCount, tierDef.MonthlyQuota, apiKey.QuotaResetAt);

        // Add quota headers to all responses
        context.Response.OnStarting(() =>
        {
            var remaining = Math.Max(0, tierDef.MonthlyQuota - currentCount);
            context.Response.Headers["X-Quota-Limit"] = tierDef.MonthlyQuota.ToString();
            context.Response.Headers["X-Quota-Remaining"] = remaining.ToString();
            context.Response.Headers["X-Quota-Resets-At"] = apiKey.QuotaResetAt.ToString("o");
            return Task.CompletedTask;
        });

        if (!allowed)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/json";
            var errorResponse = new
            {
                code = ErrorCodes.QuotaExceeded,
                message = "Monthly request quota exceeded. Your quota resets at the beginning of next month.",
                quotaResetsAt = apiKey.QuotaResetAt.ToString("o"),
                timestamp = DateTime.UtcNow.ToString("o"),
                traceId = context.TraceIdentifier,
                path = context.Request.Path.Value
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, JsonOptions));
            return;
        }

        await _next(context);
    }
}

public static class QuotaEnforcementMiddlewareExtensions
{
    public static IApplicationBuilder UseQuotaEnforcement(this IApplicationBuilder app)
    {
        return app.UseMiddleware<QuotaEnforcementMiddleware>();
    }
}

using System.Text.Json;
using PreflightApi.API.Models;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Middleware;

/// <summary>
/// Validates the X-Api-Key header on every request and sets HttpContext.Items["ApiKey"]
/// with the resolved ApiKey entity for downstream middleware.
/// Replaces GatewaySecretMiddleware.
/// </summary>
public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly bool _isDevelopment;
    private readonly bool _bypassInDevelopment;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly HashSet<string> ExemptPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/openapi",
        "/swagger",
        "/webhooks"
    };

    public ApiKeyAuthenticationMiddleware(
        RequestDelegate next,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        _next = next;
        _isDevelopment = environment.IsDevelopment();
        _bypassInDevelopment = configuration.GetValue<bool>("ApiKeyAuth:BypassInDevelopment");
    }

    public async Task InvokeAsync(HttpContext context, IApiKeyService apiKeyService)
    {
        var path = context.Request.Path;

        // Skip authentication for exempt paths
        if (IsExemptPath(path))
        {
            await _next(context);
            return;
        }

        // Skip for Clerk JWT-authenticated management endpoints (handled by ASP.NET Core auth)
        if (path.StartsWithSegments("/api") && path.Value?.Contains("/api-keys", StringComparison.OrdinalIgnoreCase) == true)
        {
            await _next(context);
            return;
        }

        // Dev bypass
        if (_isDevelopment && _bypassInDevelopment)
        {
            await _next(context);
            return;
        }

        var rawKey = context.Request.Headers["X-Api-Key"].FirstOrDefault();

        if (string.IsNullOrEmpty(rawKey))
        {
            await WriteErrorResponse(context, StatusCodes.Status401Unauthorized,
                ErrorCodes.Unauthorized, "Missing API key. Include your key in the X-Api-Key header.");
            return;
        }

        var apiKey = await apiKeyService.ValidateAsync(rawKey, context.RequestAborted);

        if (apiKey == null)
        {
            await WriteErrorResponse(context, StatusCodes.Status401Unauthorized,
                ErrorCodes.Unauthorized, "Invalid or revoked API key.");
            return;
        }

        context.Items["ApiKey"] = apiKey;

        await _next(context);
    }

    private static bool IsExemptPath(PathString path)
    {
        foreach (var prefix in ExemptPrefixes)
        {
            if (path.StartsWithSegments(prefix))
                return true;
        }
        return false;
    }

    private static async Task WriteErrorResponse(HttpContext context, int statusCode, string code, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        var errorResponse = new ApiErrorResponse
        {
            Code = code,
            Message = message,
            Timestamp = DateTime.UtcNow.ToString("o"),
            TraceId = context.TraceIdentifier,
            Path = context.Request.Path.Value
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, JsonOptions));
    }
}

public static class ApiKeyAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
    }
}

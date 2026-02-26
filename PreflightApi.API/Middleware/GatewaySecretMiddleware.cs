using System.Text.Json;
using PreflightApi.API.Models;
using PreflightApi.Domain.Exceptions;

namespace PreflightApi.API.Middleware;

/// <summary>
/// Middleware that validates requests contain the correct APIM gateway secret header.
/// Ensures only traffic routed through APIM can reach the API.
/// Skipped in Development to allow direct local testing.
/// </summary>
public class GatewaySecretMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string? _expectedSecret;
    private readonly bool _isDevelopment;

    public GatewaySecretMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _next = next;
        _expectedSecret = configuration["GatewaySecret"];
        _isDevelopment = environment.IsDevelopment();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_isDevelopment)
        {
            await _next(context);
            return;
        }

        // Allow Azure platform health probes, OpenAPI, and Swagger through without the secret.
        // /health/live and /health/ready are lightweight probes used by Azure App Service.
        // Other /health endpoints (e.g. /health, /health/data-currency) require the secret.
        if (context.Request.Path.StartsWithSegments("/health/live") ||
            context.Request.Path.StartsWithSegments("/health/ready") ||
            context.Request.Path.StartsWithSegments("/openapi") ||
            context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        var actualSecret = context.Request.Headers["X-Api-Gateway-Secret"].FirstOrDefault();

        if (string.IsNullOrEmpty(_expectedSecret) ||
            !string.Equals(_expectedSecret, actualSecret, StringComparison.Ordinal))
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            var errorResponse = new ApiErrorResponse
            {
                Code = ErrorCodes.Forbidden,
                Message = "Direct access to this API is not allowed.",
                Timestamp = DateTime.UtcNow.ToString("o"),
                TraceId = context.TraceIdentifier,
                Path = context.Request.Path.Value
            };
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                }));
            return;
        }

        await _next(context);
    }
}

public static class GatewaySecretMiddlewareExtensions
{
    public static IApplicationBuilder UseGatewaySecretValidation(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GatewaySecretMiddleware>();
    }
}

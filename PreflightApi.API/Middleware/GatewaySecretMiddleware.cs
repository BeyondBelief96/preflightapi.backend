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

        // Allow health probes, OpenAPI, and Swagger through without the secret
        if (context.Request.Path.StartsWithSegments("/health") ||
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
            await context.Response.WriteAsync("{\"error\":\"Direct access to this API is not allowed.\"}");
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

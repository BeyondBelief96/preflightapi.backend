using System.Reflection;

namespace PreflightApi.API.Middleware;

/// <summary>
/// Middleware that adds an X-API-Version header to every response.
/// The version is read once from the entry assembly's InformationalVersion attribute.
/// </summary>
public class ApiVersionHeaderMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _version;

    public ApiVersionHeaderMiddleware(RequestDelegate next)
    {
        _next = next;

        var informationalVersion = Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "unknown";

        // Strip SourceLink commit hash suffix (e.g. "1.0.0+abc123def")
        var plusIndex = informationalVersion.IndexOf('+');
        _version = plusIndex >= 0
            ? informationalVersion[..plusIndex]
            : informationalVersion;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-API-Version"] = _version;
        await _next(context);
    }
}

public static class ApiVersionHeaderMiddlewareExtensions
{
    public static IApplicationBuilder UseApiVersionHeader(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ApiVersionHeaderMiddleware>();
    }
}

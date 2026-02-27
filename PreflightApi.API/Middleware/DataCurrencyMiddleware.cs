using Microsoft.Extensions.Caching.Memory;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Middleware;

public class DataCurrencyMiddleware
{
    private readonly RequestDelegate _next;

    private const string CacheKey = "data-currency-all";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);

    public DataCurrencyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IDataSyncStatusService dataSyncStatusService, IMemoryCache memoryCache)
    {
        // Determine matching sync types from the route before the response starts
        var routeSegment = DataRouteMapping.ExtractDataRouteSegment(context.Request.Path);
        string[]? syncTypes = null;
        if (routeSegment != null)
        {
            DataRouteMapping.RouteToSyncTypes.TryGetValue(routeSegment, out syncTypes);
        }

        if (syncTypes != null)
        {
            // Use OnStarting to add headers before the response body streams out
            context.Response.OnStarting(async state =>
            {
                var (ctx, svc, cache, types) = ((HttpContext, IDataSyncStatusService, IMemoryCache, string[]))state;

                if (ctx.Response.StatusCode < 200 || ctx.Response.StatusCode >= 300)
                    return;

                try
                {
                    var allFreshness = await cache.GetOrCreateAsync(CacheKey, async entry =>
                    {
                        entry.AbsoluteExpirationRelativeToNow = CacheTtl;
                        return await svc.GetAllCurrencyAsync(CancellationToken.None);
                    });

                    if (allFreshness == null) return;

                    var relevant = allFreshness.Where(r => types.Contains(r.SyncType)).ToList();
                    if (relevant.Count == 0) return;

                    // Use worst severity for the route
                    var worst = relevant.OrderByDescending(r => SeverityRank(r.Severity)).First();

                    ctx.Response.Headers["X-Data-Currency"] = worst.IsFresh ? "fresh" : $"stale:{worst.Severity}";
                    if (worst.LastSuccessfulSync.HasValue)
                    {
                        ctx.Response.Headers["X-Data-Last-Updated"] = worst.LastSuccessfulSync.Value.ToString("o");
                    }
                    if (worst.AgeMinutes.HasValue)
                    {
                        ctx.Response.Headers["X-Data-Sync-Age-Minutes"] = worst.AgeMinutes.Value.ToString("F1");
                    }
                }
                catch
                {
                    // Don't fail the request over freshness headers
                }
            }, (context, dataSyncStatusService, memoryCache, syncTypes));
        }

        await _next(context);
    }

    private static int SeverityRank(string severity) => severity switch
    {
        "critical" => 3,
        "warning" => 2,
        "info" => 1,
        _ => 0
    };
}

public static class DataCurrencyMiddlewareExtensions
{
    public static IApplicationBuilder UseDataCurrency(this IApplicationBuilder app)
        => app.UseMiddleware<DataCurrencyMiddleware>();
}

using Microsoft.Extensions.Caching.Memory;
using PreflightApi.Domain.Constants;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Middleware;

public class DataFreshnessMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly Dictionary<string, string[]> RouteToSyncTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["metars"] = [SyncTypes.Metar],
        ["tafs"] = [SyncTypes.Taf],
        ["pireps"] = [SyncTypes.Pirep],
        ["sigmets"] = [SyncTypes.Sigmet],
        ["g-airmets"] = [SyncTypes.GAirmet],
        ["notams"] = [SyncTypes.NotamDelta],
        ["airports"] = [SyncTypes.Airport],
        ["communication-frequencies"] = [SyncTypes.Frequency],
        ["airspaces"] = [SyncTypes.Airspace, SyncTypes.SpecialUseAirspace],
        ["obstacles"] = [SyncTypes.Obstacle],
        ["chart-supplements"] = [SyncTypes.ChartSupplement],
        ["terminal-procedures"] = [SyncTypes.TerminalProcedure],
    };

    private const string CacheKey = "data-freshness-all";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);

    public DataFreshnessMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IDataSyncStatusService dataSyncStatusService, IMemoryCache memoryCache)
    {
        // Determine matching sync types from the route before the response starts
        var routeSegment = ExtractDataRouteSegment(context.Request.Path);
        string[]? syncTypes = null;
        if (routeSegment != null)
        {
            RouteToSyncTypes.TryGetValue(routeSegment, out syncTypes);
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
                        return await svc.GetAllFreshnessAsync(CancellationToken.None);
                    });

                    if (allFreshness == null) return;

                    var relevant = allFreshness.Where(r => types.Contains(r.SyncType)).ToList();
                    if (relevant.Count == 0) return;

                    // Use worst severity for the route
                    var worst = relevant.OrderByDescending(r => SeverityRank(r.Severity)).First();

                    ctx.Response.Headers["X-Data-Freshness"] = worst.IsFresh ? "fresh" : $"stale:{worst.Severity}";
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

    private static string? ExtractDataRouteSegment(PathString path)
    {
        // Routes are /api/v{version}/{segment}/...
        var value = path.Value;
        if (value == null || !value.StartsWith("/api/v", StringComparison.OrdinalIgnoreCase))
            return null;

        // Find the segment after /api/vN/
        var parts = value.Split('/', StringSplitOptions.RemoveEmptyEntries);
        // parts[0] = "api", parts[1] = "v1" (or similar), parts[2] = route segment
        return parts.Length >= 3 ? parts[2] : null;
    }

    private static int SeverityRank(string severity) => severity switch
    {
        "critical" => 3,
        "warning" => 2,
        "info" => 1,
        _ => 0
    };
}

public static class DataFreshnessMiddlewareExtensions
{
    public static IApplicationBuilder UseDataFreshness(this IApplicationBuilder app)
        => app.UseMiddleware<DataFreshnessMiddleware>();
}

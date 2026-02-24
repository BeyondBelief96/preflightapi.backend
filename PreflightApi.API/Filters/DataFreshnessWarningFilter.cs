using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.API.Filters;

public class DataFreshnessWarningFilter : IAsyncResultFilter
{
    private const string AcceptWarningsHeader = "Accept-Warnings";
    private const string AcceptWarningsValue = "stale-data";
    private const string CacheKey = "data-freshness-all";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        // Only wrap if the client opted in via Accept-Warnings: stale-data
        if (!context.HttpContext.Request.Headers.TryGetValue(AcceptWarningsHeader, out var headerValue)
            || !string.Equals(headerValue, AcceptWarningsValue, StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        // Only wrap ObjectResult with 2xx status
        if (context.Result is not ObjectResult objectResult
            || objectResult.StatusCode is not (null or (>= 200 and < 300)))
        {
            await next();
            return;
        }

        // Check if the route matches a data endpoint
        var routeSegment = DataRouteMapping.ExtractDataRouteSegment(context.HttpContext.Request.Path);
        if (routeSegment == null || !DataRouteMapping.RouteToSyncTypes.TryGetValue(routeSegment, out var syncTypes))
        {
            await next();
            return;
        }

        try
        {
            var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
            var syncService = context.HttpContext.RequestServices.GetRequiredService<IDataSyncStatusService>();

            var allFreshness = await cache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheTtl;
                return await syncService.GetAllFreshnessAsync(CancellationToken.None);
            });

            if (allFreshness != null)
            {
                var staleItems = allFreshness
                    .Where(r => syncTypes.Contains(r.SyncType) && !r.IsFresh)
                    .ToList();

                if (staleItems.Count > 0)
                {
                    var warnings = staleItems.Select(item => new DataWarning
                    {
                        SyncType = item.SyncType,
                        Severity = item.Severity,
                        Message = item.Message,
                        LastSuccessfulSync = item.LastSuccessfulSync
                    }).ToList();

                    objectResult.Value = new
                    {
                        data = objectResult.Value,
                        warnings
                    };
                }
            }
        }
        catch
        {
            // Don't fail the request over warning wrapping
        }

        await next();
    }
}

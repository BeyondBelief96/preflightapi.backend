using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Infrastructure.Services;

public class QuotaTrackingService : IQuotaTrackingService
{
    private readonly ConcurrentDictionary<Guid, QuotaCounter> _counters = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<QuotaTrackingService> _logger;

    public QuotaTrackingService(IServiceScopeFactory scopeFactory, ILogger<QuotaTrackingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public (bool Allowed, long CurrentCount) IncrementAndCheck(Guid apiKeyId, long dbCount, int monthlyLimit, DateTime quotaResetAt)
    {
        var counter = _counters.GetOrAdd(apiKeyId, _ => new QuotaCounter(dbCount, quotaResetAt));

        // Check if quota period has reset
        if (DateTime.UtcNow >= counter.QuotaResetAt)
        {
            counter.Reset(quotaResetAt.AddMonths(1));
        }

        var newCount = counter.Increment();

        if (newCount > monthlyLimit)
        {
            counter.Decrement();
            return (false, newCount - 1);
        }

        return (true, newCount);
    }

    public long GetCurrentCount(Guid apiKeyId)
    {
        return _counters.TryGetValue(apiKeyId, out var counter) ? counter.Count : 0;
    }

    public async Task FlushToDatabaseAsync(CancellationToken ct = default)
    {
        var dirtyEntries = _counters
            .Where(kvp => kvp.Value.IsDirty)
            .ToList();

        if (dirtyEntries.Count == 0)
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PreflightApiDbContext>();

            foreach (var (apiKeyId, counter) in dirtyEntries)
            {
                var count = counter.Count;
                var resetAt = counter.QuotaResetAt;

                await context.ApiKeys
                    .Where(k => k.Id == apiKeyId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(k => k.MonthlyRequestCount, count)
                        .SetProperty(k => k.QuotaResetAt, resetAt), ct);

                counter.MarkClean();
            }

            _logger.LogDebug("Flushed quota counters for {Count} API keys", dirtyEntries.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flush quota counters to database");
        }
    }

    internal sealed class QuotaCounter
    {
        private long _count;
        private int _dirty;

        public DateTime QuotaResetAt { get; private set; }

        public long Count => Interlocked.Read(ref _count);
        public bool IsDirty => Interlocked.CompareExchange(ref _dirty, 0, 0) == 1;

        public QuotaCounter(long initialCount, DateTime quotaResetAt)
        {
            _count = initialCount;
            QuotaResetAt = quotaResetAt;
        }

        public long Increment()
        {
            Interlocked.Exchange(ref _dirty, 1);
            return Interlocked.Increment(ref _count);
        }

        public void Decrement()
        {
            Interlocked.Decrement(ref _count);
        }

        public void Reset(DateTime nextResetAt)
        {
            Interlocked.Exchange(ref _count, 0);
            Interlocked.Exchange(ref _dirty, 1);
            QuotaResetAt = nextResetAt;
        }

        public void MarkClean()
        {
            Interlocked.Exchange(ref _dirty, 0);
        }
    }
}

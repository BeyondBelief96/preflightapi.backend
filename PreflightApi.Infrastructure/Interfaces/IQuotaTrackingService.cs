namespace PreflightApi.Infrastructure.Interfaces;

public interface IQuotaTrackingService
{
    /// <summary>
    /// Increments the request counter for the given API key and checks against the monthly limit.
    /// Returns whether the request is allowed and the current count.
    /// </summary>
    (bool Allowed, long CurrentCount) IncrementAndCheck(Guid apiKeyId, long dbCount, int monthlyLimit, DateTime quotaResetAt);

    /// <summary>
    /// Gets the current in-memory request count for the given API key.
    /// </summary>
    long GetCurrentCount(Guid apiKeyId);

    /// <summary>
    /// Flushes all dirty in-memory counters to the database.
    /// </summary>
    Task FlushToDatabaseAsync(CancellationToken ct = default);
}

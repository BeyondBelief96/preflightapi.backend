namespace PreflightApi.Infrastructure.Interfaces;

/// <summary>
/// Syncs NOTAM deltas from the FAA NMS API into the local database.
/// Runs every few minutes, fetching only NOTAMs updated since the last sync.
/// </summary>
public interface INotamDeltaSyncCronService
{
    Task SyncDeltaAsync(CancellationToken ct = default);
}

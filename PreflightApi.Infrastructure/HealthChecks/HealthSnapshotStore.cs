using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace PreflightApi.Infrastructure.HealthChecks;

public record SmoothedHealthCheckEntry(
    string Name,
    HealthStatus Status,
    double Duration,
    string? Description,
    IEnumerable<string> Tags,
    string? Exception);

public record HealthSnapshot(
    HealthStatus OverallStatus,
    DateTime LastCheckedAt,
    double TotalDuration,
    IReadOnlyList<SmoothedHealthCheckEntry> Entries);

public interface IHealthSnapshotStore
{
    HealthSnapshot? Current { get; }
}

public class HealthSnapshotStore : IHealthSnapshotStore
{
    private readonly int _failureThreshold;
    private readonly int _recoveryThreshold;
    private readonly object _lock = new();
    private readonly Dictionary<string, CheckStateTracker> _trackers = new();
    private HealthSnapshot? _current;

    public HealthSnapshotStore(IOptions<HealthMonitorSettings> settings)
    {
        _failureThreshold = settings.Value.FailureThreshold;
        _recoveryThreshold = settings.Value.RecoveryThreshold;
    }

    public HealthSnapshot? Current
    {
        get { lock (_lock) { return _current; } }
    }

    public void Update(HealthReport report)
    {
        lock (_lock)
        {
            var entries = new List<SmoothedHealthCheckEntry>();

            foreach (var (name, entry) in report.Entries)
            {
                if (!_trackers.TryGetValue(name, out var tracker))
                {
                    tracker = new CheckStateTracker();
                    _trackers[name] = tracker;
                }

                var smoothedStatus = tracker.Apply(entry.Status, _failureThreshold, _recoveryThreshold);

                entries.Add(new SmoothedHealthCheckEntry(
                    name,
                    smoothedStatus,
                    entry.Duration.TotalMilliseconds,
                    entry.Description,
                    entry.Tags,
                    entry.Exception?.Message));
            }

            var overallStatus = entries.Count > 0
                ? entries.Min(e => e.Status)
                : HealthStatus.Healthy;

            _current = new HealthSnapshot(
                overallStatus,
                DateTime.UtcNow,
                report.TotalDuration.TotalMilliseconds,
                entries);
        }
    }

    internal class CheckStateTracker
    {
        private HealthStatus? _smoothedStatus;
        private int _consecutiveFailures;
        private int _consecutiveSuccesses;

        public HealthStatus Apply(HealthStatus rawStatus, int failureThreshold, int recoveryThreshold)
        {
            // First run: accept raw result immediately
            if (_smoothedStatus is null)
            {
                _smoothedStatus = rawStatus;
                _consecutiveFailures = rawStatus != HealthStatus.Healthy ? 1 : 0;
                _consecutiveSuccesses = rawStatus == HealthStatus.Healthy ? 1 : 0;
                return rawStatus;
            }

            if (rawStatus == HealthStatus.Healthy)
            {
                _consecutiveSuccesses++;
                _consecutiveFailures = 0;

                if (_consecutiveSuccesses >= recoveryThreshold)
                {
                    _smoothedStatus = HealthStatus.Healthy;
                }
            }
            else
            {
                _consecutiveFailures++;
                _consecutiveSuccesses = 0;

                if (_consecutiveFailures >= failureThreshold)
                {
                    // Use the worst (lowest enum value) status seen
                    _smoothedStatus = rawStatus < _smoothedStatus ? rawStatus : _smoothedStatus;
                }
            }

            return _smoothedStatus.Value;
        }
    }
}

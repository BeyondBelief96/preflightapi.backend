using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using PreflightApi.Infrastructure.HealthChecks;
using Xunit;

namespace PreflightApi.Tests.HealthCheckTests;

public class HealthSnapshotStoreTests
{
    private readonly HealthSnapshotStore _store;

    public HealthSnapshotStoreTests()
    {
        var settings = Options.Create(new HealthMonitorSettings
        {
            FailureThreshold = 3,
            RecoveryThreshold = 2
        });
        _store = new HealthSnapshotStore(settings);
    }

    [Fact]
    public void Current_BeforeFirstUpdate_ReturnsNull()
    {
        _store.Current.Should().BeNull();
    }

    [Fact]
    public void FirstUpdate_AcceptsRawResultImmediately()
    {
        var report = BuildReport(("db", HealthStatus.Healthy), ("api", HealthStatus.Degraded));

        _store.Update(report);

        var snapshot = _store.Current!;
        snapshot.Entries.Should().HaveCount(2);
        snapshot.Entries[0].Status.Should().Be(HealthStatus.Healthy);
        snapshot.Entries[1].Status.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public void SingleTransientFailure_DoesNotChangeSmoothedStatus()
    {
        // Start healthy
        _store.Update(BuildReport(("db", HealthStatus.Healthy)));
        _store.Current!.Entries[0].Status.Should().Be(HealthStatus.Healthy);

        // One failure — still healthy (threshold = 3)
        _store.Update(BuildReport(("db", HealthStatus.Degraded)));
        _store.Current!.Entries[0].Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public void ConsecutiveFailuresAtThreshold_TransitionsStatus()
    {
        _store.Update(BuildReport(("db", HealthStatus.Healthy)));

        // 3 consecutive failures = threshold
        _store.Update(BuildReport(("db", HealthStatus.Degraded)));
        _store.Update(BuildReport(("db", HealthStatus.Degraded)));
        _store.Update(BuildReport(("db", HealthStatus.Degraded)));

        _store.Current!.Entries[0].Status.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public void RecoveryRequiresConsecutiveSuccesses()
    {
        // Drive to degraded
        _store.Update(BuildReport(("db", HealthStatus.Healthy)));
        _store.Update(BuildReport(("db", HealthStatus.Degraded)));
        _store.Update(BuildReport(("db", HealthStatus.Degraded)));
        _store.Update(BuildReport(("db", HealthStatus.Degraded)));
        _store.Current!.Entries[0].Status.Should().Be(HealthStatus.Degraded);

        // 1 success — not enough (threshold = 2)
        _store.Update(BuildReport(("db", HealthStatus.Healthy)));
        _store.Current!.Entries[0].Status.Should().Be(HealthStatus.Degraded);

        // 2nd success — recovers
        _store.Update(BuildReport(("db", HealthStatus.Healthy)));
        _store.Current!.Entries[0].Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public void FailureCounterResetsOnSuccess()
    {
        _store.Update(BuildReport(("db", HealthStatus.Healthy)));

        // 2 failures, then 1 success resets counter
        _store.Update(BuildReport(("db", HealthStatus.Degraded)));
        _store.Update(BuildReport(("db", HealthStatus.Degraded)));
        _store.Update(BuildReport(("db", HealthStatus.Healthy)));

        // 2 more failures — still healthy because counter was reset
        _store.Update(BuildReport(("db", HealthStatus.Degraded)));
        _store.Update(BuildReport(("db", HealthStatus.Degraded)));
        _store.Current!.Entries[0].Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public void SuccessCounterResetsOnFailure()
    {
        // Drive to degraded
        _store.Update(BuildReport(("db", HealthStatus.Healthy)));
        _store.Update(BuildReport(("db", HealthStatus.Degraded)));
        _store.Update(BuildReport(("db", HealthStatus.Degraded)));
        _store.Update(BuildReport(("db", HealthStatus.Degraded)));
        _store.Current!.Entries[0].Status.Should().Be(HealthStatus.Degraded);

        // 1 success, then failure resets success counter
        _store.Update(BuildReport(("db", HealthStatus.Healthy)));
        _store.Update(BuildReport(("db", HealthStatus.Degraded)));

        // 1 more success — only 1 consecutive, not enough
        _store.Update(BuildReport(("db", HealthStatus.Healthy)));
        _store.Current!.Entries[0].Status.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public void OverallStatus_IsWorstOfAllChecks()
    {
        _store.Update(BuildReport(
            ("db", HealthStatus.Healthy),
            ("api", HealthStatus.Degraded),
            ("blob", HealthStatus.Unhealthy)));

        _store.Current!.OverallStatus.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public void OverallStatus_AllHealthy_ReturnsHealthy()
    {
        _store.Update(BuildReport(
            ("db", HealthStatus.Healthy),
            ("api", HealthStatus.Healthy)));

        _store.Current!.OverallStatus.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public void IndependentTrackingPerCheck()
    {
        // Both start healthy
        _store.Update(BuildReport(("db", HealthStatus.Healthy), ("api", HealthStatus.Healthy)));

        // Only 'db' fails 3x — 'api' stays healthy
        _store.Update(BuildReport(("db", HealthStatus.Degraded), ("api", HealthStatus.Healthy)));
        _store.Update(BuildReport(("db", HealthStatus.Degraded), ("api", HealthStatus.Healthy)));
        _store.Update(BuildReport(("db", HealthStatus.Degraded), ("api", HealthStatus.Healthy)));

        var snapshot = _store.Current!;
        snapshot.Entries.First(e => e.Name == "db").Status.Should().Be(HealthStatus.Degraded);
        snapshot.Entries.First(e => e.Name == "api").Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public void Update_SetsTotalDurationAndLastCheckedAt()
    {
        var before = DateTime.UtcNow;
        _store.Update(BuildReport(("db", HealthStatus.Healthy)));
        var after = DateTime.UtcNow;

        var snapshot = _store.Current!;
        snapshot.TotalDuration.Should().BeGreaterThanOrEqualTo(0);
        snapshot.LastCheckedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Update_PreservesEntryMetadata()
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["db"] = new(
                HealthStatus.Healthy,
                "Connection OK",
                TimeSpan.FromMilliseconds(42),
                null,
                null,
                new[] { "ready" })
        };
        var report = new HealthReport(entries, TimeSpan.FromMilliseconds(42));

        _store.Update(report);

        var entry = _store.Current!.Entries[0];
        entry.Name.Should().Be("db");
        entry.Description.Should().Be("Connection OK");
        entry.Duration.Should().Be(42);
        entry.Tags.Should().Contain("ready");
    }

    private static HealthReport BuildReport(params (string name, HealthStatus status)[] checks)
    {
        var entries = new Dictionary<string, HealthReportEntry>();
        foreach (var (name, status) in checks)
        {
            entries[name] = new HealthReportEntry(
                status,
                description: null,
                duration: TimeSpan.FromMilliseconds(10),
                exception: null,
                data: null);
        }
        return new HealthReport(entries, TimeSpan.FromMilliseconds(50));
    }
}

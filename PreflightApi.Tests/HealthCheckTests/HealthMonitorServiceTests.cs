using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PreflightApi.Infrastructure.HealthChecks;
using Xunit;

namespace PreflightApi.Tests.HealthCheckTests;

public class HealthMonitorServiceTests
{
    private readonly HealthCheckService _healthCheckService;
    private readonly HealthSnapshotStore _store;
    private readonly HealthMonitorService _service;

    public HealthMonitorServiceTests()
    {
        _healthCheckService = Substitute.For<HealthCheckService>();
        var settings = Options.Create(new HealthMonitorSettings
        {
            IntervalSeconds = 1,
            FailureThreshold = 3,
            RecoveryThreshold = 2
        });
        _store = new HealthSnapshotStore(settings);
        _service = new HealthMonitorService(
            _healthCheckService,
            _store,
            settings,
            NullLogger<HealthMonitorService>.Instance);
    }

    [Fact]
    public async Task ExecuteAsync_RunsFirstCheckImmediately()
    {
        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["db"] = new(HealthStatus.Healthy, null, TimeSpan.FromMilliseconds(5), null, null)
            },
            TimeSpan.FromMilliseconds(5));

        // Mock the abstract overload (predicate + cancellation token)
        _healthCheckService.CheckHealthAsync(
                Arg.Any<Func<HealthCheckRegistration, bool>?>(),
                Arg.Any<CancellationToken>())
            .Returns(report);

        using var cts = new CancellationTokenSource();

        // Start the service, then cancel after a short delay
        await _service.StartAsync(cts.Token);
        await Task.Delay(200);
        cts.Cancel();

        try { await _service.ExecuteTask!; } catch (OperationCanceledException) { }

        _store.Current.Should().NotBeNull();
        _store.Current!.OverallStatus.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesExceptionsWithoutCrashing()
    {
        _healthCheckService.CheckHealthAsync(
                Arg.Any<Func<HealthCheckRegistration, bool>?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));

        using var cts = new CancellationTokenSource();

        await _service.StartAsync(cts.Token);
        await Task.Delay(200);
        cts.Cancel();

        // Should not throw — exception is caught internally
        Func<Task> act = async () =>
        {
            try { await _service.ExecuteTask!; } catch (OperationCanceledException) { }
        };
        await act.Should().NotThrowAsync();

        // Store should remain null since check never succeeded
        _store.Current.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_StopsCleanlyOnCancellation()
    {
        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>(),
            TimeSpan.Zero);

        _healthCheckService.CheckHealthAsync(
                Arg.Any<Func<HealthCheckRegistration, bool>?>(),
                Arg.Any<CancellationToken>())
            .Returns(report);

        using var cts = new CancellationTokenSource();
        await _service.StartAsync(cts.Token);
        await Task.Delay(100);
        cts.Cancel();

        Func<Task> act = async () =>
        {
            try { await _service.ExecuteTask!; } catch (OperationCanceledException) { }
        };
        await act.Should().NotThrowAsync();
    }
}

using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.HealthChecks;
using PreflightApi.Infrastructure.Interfaces;
using Xunit;

namespace PreflightApi.Tests.HealthCheckTests;

public class DataFreshnessHealthCheckTests
{
    private readonly IDataSyncStatusService _syncService;
    private readonly DataFreshnessHealthCheck _healthCheck;

    public DataFreshnessHealthCheckTests()
    {
        _syncService = Substitute.For<IDataSyncStatusService>();
        _healthCheck = new DataFreshnessHealthCheck(_syncService);
    }

    private HealthCheckContext CreateContext()
    {
        return new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("data-freshness", _healthCheck, null, null)
        };
    }

    private static DataFreshnessResult MakeResult(string syncType, bool isFresh, string severity = "none")
    {
        return new DataFreshnessResult
        {
            SyncType = syncType,
            IsFresh = isFresh,
            Severity = severity,
            StalenessMode = "TimeBased",
            Message = $"{syncType} {(isFresh ? "is fresh" : "is stale")}",
            LastSuccessfulSync = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task CheckHealth_AllFresh_ReturnsHealthy()
    {
        // Arrange
        _syncService.GetAllFreshnessAsync(Arg.Any<CancellationToken>())
            .Returns(new List<DataFreshnessResult>
            {
                MakeResult("Metar", isFresh: true),
                MakeResult("Taf", isFresh: true)
            }.AsReadOnly());

        // Act
        var result = await _healthCheck.CheckHealthAsync(CreateContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("fresh");
    }

    [Fact]
    public async Task CheckHealth_SomeStale_ReturnsDegraded()
    {
        // Arrange
        _syncService.GetAllFreshnessAsync(Arg.Any<CancellationToken>())
            .Returns(new List<DataFreshnessResult>
            {
                MakeResult("Metar", isFresh: true),
                MakeResult("Taf", isFresh: false, severity: "warning")
            }.AsReadOnly());

        // Act
        var result = await _healthCheck.CheckHealthAsync(CreateContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("Taf");
    }

    [Fact]
    public async Task CheckHealth_StaleTypesIncludedInData()
    {
        // Arrange
        _syncService.GetAllFreshnessAsync(Arg.Any<CancellationToken>())
            .Returns(new List<DataFreshnessResult>
            {
                MakeResult("Metar", isFresh: false, severity: "warning"),
                MakeResult("Taf", isFresh: false, severity: "critical")
            }.AsReadOnly());

        // Act
        var result = await _healthCheck.CheckHealthAsync(CreateContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Data.Should().ContainKey("Metar");
        result.Data.Should().ContainKey("Taf");
    }

    [Fact]
    public async Task CheckHealth_ServiceThrows_ReturnsDegraded()
    {
        // Arrange
        _syncService.GetAllFreshnessAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("DB error"));

        // Act
        var result = await _healthCheck.CheckHealthAsync(CreateContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("Unable to evaluate");
    }
}

using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.HealthChecks;
using PreflightApi.Infrastructure.Interfaces;
using Xunit;

namespace PreflightApi.Tests.HealthCheckTests;

public class DataCurrencyHealthCheckTests
{
    private readonly IDataSyncStatusService _syncService;
    private readonly DataCurrencyHealthCheck _healthCheck;

    public DataCurrencyHealthCheckTests()
    {
        _syncService = Substitute.For<IDataSyncStatusService>();
        _healthCheck = new DataCurrencyHealthCheck(_syncService);
    }

    private HealthCheckContext CreateContext()
    {
        return new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("data-currency", _healthCheck, null, null)
        };
    }

    private static DataCurrencyResult MakeResult(string syncType, bool isFresh, string severity = "none")
    {
        return new DataCurrencyResult
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
        _syncService.GetAllCurrencyAsync(Arg.Any<CancellationToken>())
            .Returns(new List<DataCurrencyResult>
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
        _syncService.GetAllCurrencyAsync(Arg.Any<CancellationToken>())
            .Returns(new List<DataCurrencyResult>
            {
                MakeResult("Metar", isFresh: true),
                MakeResult("Taf", isFresh: false, severity: "warning")
            }.AsReadOnly());

        // Act
        var result = await _healthCheck.CheckHealthAsync(CreateContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("1 data source is stale");
        result.Description.Should().Contain("Data Currency");
    }

    [Fact]
    public async Task CheckHealth_MultipleStale_ShowsCount()
    {
        // Arrange
        _syncService.GetAllCurrencyAsync(Arg.Any<CancellationToken>())
            .Returns(new List<DataCurrencyResult>
            {
                MakeResult("Metar", isFresh: false, severity: "warning"),
                MakeResult("Taf", isFresh: false, severity: "critical")
            }.AsReadOnly());

        // Act
        var result = await _healthCheck.CheckHealthAsync(CreateContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("2 data sources are stale");
        result.Description.Should().Contain("Data Currency");
    }

    [Fact]
    public async Task CheckHealth_ServiceThrows_ReturnsDegraded()
    {
        // Arrange
        _syncService.GetAllCurrencyAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("DB error"));

        // Act
        var result = await _healthCheck.CheckHealthAsync(CreateContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("Unable to evaluate");
    }
}

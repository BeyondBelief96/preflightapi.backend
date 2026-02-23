using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PreflightApi.Infrastructure.HealthChecks;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Settings;
using Xunit;

namespace PreflightApi.Tests.HealthCheckTests;

public class BlobStorageHealthCheckTests
{
    private readonly ICloudStorageService _cloudStorageService;
    private readonly BlobStorageHealthCheck _healthCheck;

    public BlobStorageHealthCheckTests()
    {
        _cloudStorageService = Substitute.For<ICloudStorageService>();
        var settings = Options.Create(new CloudStorageSettings
        {
            ChartSupplementsContainerName = "chart-supplements"
        });
        _healthCheck = new BlobStorageHealthCheck(_cloudStorageService, settings);
    }

    [Fact]
    public async Task CheckHealthAsync_ContainerExists_ReturnsHealthy()
    {
        // Arrange
        _cloudStorageService.ContainerExistsAsync("chart-supplements").Returns(true);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("blob-storage", _healthCheck, null, null)
        };

        // Act
        var result = await _healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("reachable");
    }

    [Fact]
    public async Task CheckHealthAsync_ContainerDoesNotExist_ReturnsDegraded()
    {
        // Arrange
        _cloudStorageService.ContainerExistsAsync("chart-supplements").Returns(false);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("blob-storage", _healthCheck, null, null)
        };

        // Act
        var result = await _healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("does not exist");
    }

    [Fact]
    public async Task CheckHealthAsync_ServiceThrows_ReturnsDegraded()
    {
        // Arrange
        _cloudStorageService.ContainerExistsAsync("chart-supplements")
            .ThrowsAsync(new HttpRequestException("Connection refused"));
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("blob-storage", _healthCheck, null, null)
        };

        // Act
        var result = await _healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Exception.Should().BeOfType<HttpRequestException>();
    }
}

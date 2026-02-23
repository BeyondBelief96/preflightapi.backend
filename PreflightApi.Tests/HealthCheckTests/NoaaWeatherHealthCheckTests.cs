using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using PreflightApi.Infrastructure.HealthChecks;
using PreflightApi.Infrastructure.Utilities;
using RichardSzalay.MockHttp;
using Xunit;

namespace PreflightApi.Tests.HealthCheckTests;

public class NoaaWeatherHealthCheckTests
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly NoaaWeatherHealthCheck _healthCheck;

    public NoaaWeatherHealthCheckTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(ServiceCollectionExtensions.WeatherHttpClient)
            .Returns(_ => _mockHttp.ToHttpClient());
        _healthCheck = new NoaaWeatherHealthCheck(httpClientFactory);
    }

    [Fact]
    public async Task CheckHealthAsync_HeadReturns200_ReturnsHealthy()
    {
        // Arrange
        _mockHttp.When(HttpMethod.Head, "https://aviationweather.gov/data/cache/metars.cache.xml.gz")
            .Respond(HttpStatusCode.OK);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("noaa-weather", _healthCheck, null, null)
        };

        // Act
        var result = await _healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("reachable");
    }

    [Fact]
    public async Task CheckHealthAsync_HeadReturns503_ReturnsDegraded()
    {
        // Arrange
        _mockHttp.When(HttpMethod.Head, "https://aviationweather.gov/data/cache/metars.cache.xml.gz")
            .Respond(HttpStatusCode.ServiceUnavailable);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("noaa-weather", _healthCheck, null, null)
        };

        // Act
        var result = await _healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("503");
    }

    [Fact]
    public async Task CheckHealthAsync_RequestThrows_ReturnsDegraded()
    {
        // Arrange
        _mockHttp.When(HttpMethod.Head, "https://aviationweather.gov/data/cache/metars.cache.xml.gz")
            .Throw(new HttpRequestException("DNS resolution failed"));
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("noaa-weather", _healthCheck, null, null)
        };

        // Act
        var result = await _healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Exception.Should().BeOfType<HttpRequestException>();
    }
}

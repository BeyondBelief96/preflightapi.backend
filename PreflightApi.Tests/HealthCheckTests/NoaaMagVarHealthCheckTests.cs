using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NSubstitute;
using PreflightApi.Infrastructure.HealthChecks;
using PreflightApi.Infrastructure.Settings;
using PreflightApi.Infrastructure.Utilities;
using RichardSzalay.MockHttp;
using Xunit;

namespace PreflightApi.Tests.HealthCheckTests;

public class NoaaMagVarHealthCheckTests
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly NoaaMagVarHealthCheck _healthCheck;
    private const string ApiKey = "test-api-key";

    public NoaaMagVarHealthCheckTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(ServiceCollectionExtensions.MagVarHttpClient)
            .Returns(_ => _mockHttp.ToHttpClient());
        var settings = Options.Create(new NOAASettings { NOAAApiKey = ApiKey });
        _healthCheck = new NoaaMagVarHealthCheck(httpClientFactory, settings);
    }

    [Fact]
    public async Task CheckHealthAsync_GetReturns200_ReturnsHealthy()
    {
        // Arrange
        _mockHttp.When(HttpMethod.Get, "https://www.ngdc.noaa.gov/geomag-web/calculators/calculateDeclination*")
            .Respond(HttpStatusCode.OK);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("noaa-magvar", _healthCheck, null, null)
        };

        // Act
        var result = await _healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("reachable");
    }

    [Fact]
    public async Task CheckHealthAsync_GetReturns500_ReturnsDegraded()
    {
        // Arrange
        _mockHttp.When(HttpMethod.Get, "https://www.ngdc.noaa.gov/geomag-web/calculators/calculateDeclination*")
            .Respond(HttpStatusCode.InternalServerError);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("noaa-magvar", _healthCheck, null, null)
        };

        // Act
        var result = await _healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("500");
    }

    [Fact]
    public async Task CheckHealthAsync_RequestThrows_ReturnsDegraded()
    {
        // Arrange
        _mockHttp.When(HttpMethod.Get, "https://www.ngdc.noaa.gov/geomag-web/calculators/calculateDeclination*")
            .Throw(new HttpRequestException("Connection refused"));
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("noaa-magvar", _healthCheck, null, null)
        };

        // Act
        var result = await _healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Exception.Should().BeOfType<HttpRequestException>();
    }
}

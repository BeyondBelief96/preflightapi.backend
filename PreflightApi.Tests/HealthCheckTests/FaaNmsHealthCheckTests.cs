using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NSubstitute;
using PreflightApi.Infrastructure.HealthChecks;
using PreflightApi.Infrastructure.Settings;
using RichardSzalay.MockHttp;
using Xunit;

namespace PreflightApi.Tests.HealthCheckTests;

public class FaaNmsHealthCheckTests
{
    private const string AuthBaseUrl = "https://api-nms.aim.faa.gov";

    private static FaaNmsHealthCheck CreateHealthCheck(MockHttpMessageHandler mockHttp)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(mockHttp.ToHttpClient());

        var settings = Options.Create(new NmsSettings { AuthBaseUrl = AuthBaseUrl });
        return new FaaNmsHealthCheck(factory, settings);
    }

    private static HealthCheckContext CreateContext(FaaNmsHealthCheck hc)
    {
        return new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("faa-nms", hc, null, null)
        };
    }

    [Fact]
    public async Task CheckHealth_ServiceReachable_ReturnsHealthy()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(AuthBaseUrl).Respond(System.Net.HttpStatusCode.OK);
        var hc = CreateHealthCheck(mockHttp);

        // Act
        var result = await hc.CheckHealthAsync(CreateContext(hc));

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("reachable");
    }

    [Fact]
    public async Task CheckHealth_ServiceReturns401_StillHealthy()
    {
        // Arrange — 401 is expected (no auth), but proves service is up
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(AuthBaseUrl).Respond(System.Net.HttpStatusCode.Unauthorized);
        var hc = CreateHealthCheck(mockHttp);

        // Act
        var result = await hc.CheckHealthAsync(CreateContext(hc));

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("401");
    }

    [Fact]
    public async Task CheckHealth_ServiceUnreachable_ReturnsDegraded()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(AuthBaseUrl).Throw(new HttpRequestException("Connection refused"));
        var hc = CreateHealthCheck(mockHttp);

        // Act
        var result = await hc.CheckHealthAsync(CreateContext(hc));

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("unreachable");
    }
}

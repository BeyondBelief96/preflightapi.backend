using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using PreflightApi.API.Middleware;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Tests.ApiKeyTests;

public class ApiKeyAuthenticationMiddlewareTests
{
    private readonly IApiKeyService _apiKeyService;

    private static readonly ApiKey ValidApiKey = new()
    {
        Id = Guid.NewGuid(),
        OwnerId = "user_123",
        Tier = SubscriptionTier.PrivatePilot,
        IsActive = true,
        Prefix = "pfa_sk_test1",
        KeyHash = "testhash",
        Name = "Test Key"
    };

    public ApiKeyAuthenticationMiddlewareTests()
    {
        _apiKeyService = Substitute.For<IApiKeyService>();
    }

    // ─── Successful Authentication ──────────────────────────────────────────

    [Fact]
    public async Task ShouldSetApiKeyInContext_WhenValidKeyProvided()
    {
        // Arrange
        _apiKeyService.ValidateAsync("pfa_sk_validkey123", Arg.Any<CancellationToken>())
            .Returns(ValidApiKey);

        var (middleware, context, nextCalled) = CreateMiddleware(
            path: "/api/v1/metars",
            apiKey: "pfa_sk_validkey123",
            isDevelopment: false,
            bypassInDev: false);

        // Act
        await middleware.InvokeAsync(context, _apiKeyService);

        // Assert
        nextCalled().Should().BeTrue();
        context.Items["ApiKey"].Should().Be(ValidApiKey);
    }

    // ─── Missing Key ────────────────────────────────────────────────────────

    [Fact]
    public async Task ShouldReturn401_WhenNoKeyProvided()
    {
        // Arrange
        var (middleware, context, nextCalled) = CreateMiddleware(
            path: "/api/v1/metars",
            apiKey: null,
            isDevelopment: false,
            bypassInDev: false);

        // Act
        await middleware.InvokeAsync(context, _apiKeyService);

        // Assert
        nextCalled().Should().BeFalse();
        context.Response.StatusCode.Should().Be(401);
    }

    // ─── Invalid Key ────────────────────────────────────────────────────────

    [Fact]
    public async Task ShouldReturn401_WhenKeyIsInvalid()
    {
        // Arrange
        _apiKeyService.ValidateAsync("pfa_sk_badkey", Arg.Any<CancellationToken>())
            .Returns((ApiKey?)null);

        var (middleware, context, nextCalled) = CreateMiddleware(
            path: "/api/v1/metars",
            apiKey: "pfa_sk_badkey",
            isDevelopment: false,
            bypassInDev: false);

        // Act
        await middleware.InvokeAsync(context, _apiKeyService);

        // Assert
        nextCalled().Should().BeFalse();
        context.Response.StatusCode.Should().Be(401);
    }

    // ─── Exempt Paths ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/data-currency")]
    [InlineData("/openapi/v1.json")]
    [InlineData("/swagger/index.html")]
    [InlineData("/webhooks/stripe")]
    public async Task ShouldPassThrough_ForExemptPaths(string path)
    {
        // Arrange
        var (middleware, context, nextCalled) = CreateMiddleware(
            path: path,
            apiKey: null,
            isDevelopment: false,
            bypassInDev: false);

        // Act
        await middleware.InvokeAsync(context, _apiKeyService);

        // Assert
        nextCalled().Should().BeTrue();
        context.Response.StatusCode.Should().NotBe(401);
    }

    // ─── API Key Management Endpoints (Clerk JWT) ───────────────────────────

    [Fact]
    public async Task ShouldPassThrough_ForApiKeyManagementEndpoints()
    {
        // Arrange
        var (middleware, context, nextCalled) = CreateMiddleware(
            path: "/api/v1/api-keys",
            apiKey: null,
            isDevelopment: false,
            bypassInDev: false);

        // Act
        await middleware.InvokeAsync(context, _apiKeyService);

        // Assert
        nextCalled().Should().BeTrue();
    }

    // ─── Development Bypass ─────────────────────────────────────────────────

    [Fact]
    public async Task ShouldBypassAuth_WhenDevelopmentAndBypassEnabled()
    {
        // Arrange
        var (middleware, context, nextCalled) = CreateMiddleware(
            path: "/api/v1/metars",
            apiKey: null,
            isDevelopment: true,
            bypassInDev: true);

        // Act
        await middleware.InvokeAsync(context, _apiKeyService);

        // Assert
        nextCalled().Should().BeTrue();
    }

    [Fact]
    public async Task ShouldNotBypass_WhenDevelopmentButBypassDisabled()
    {
        // Arrange
        var (middleware, context, nextCalled) = CreateMiddleware(
            path: "/api/v1/metars",
            apiKey: null,
            isDevelopment: true,
            bypassInDev: false);

        // Act
        await middleware.InvokeAsync(context, _apiKeyService);

        // Assert
        nextCalled().Should().BeFalse();
        context.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task ShouldNotBypass_WhenProductionEvenIfBypassEnabled()
    {
        // Arrange — bypass flag set but environment is NOT development
        var (middleware, context, nextCalled) = CreateMiddleware(
            path: "/api/v1/metars",
            apiKey: null,
            isDevelopment: false,
            bypassInDev: true);

        // Act
        await middleware.InvokeAsync(context, _apiKeyService);

        // Assert
        nextCalled().Should().BeFalse();
        context.Response.StatusCode.Should().Be(401);
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private static (ApiKeyAuthenticationMiddleware middleware, HttpContext context, Func<bool> nextCalled)
        CreateMiddleware(string path, string? apiKey, bool isDevelopment, bool bypassInDev)
    {
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(isDevelopment ? "Development" : "Production");

        var configData = new Dictionary<string, string?>
        {
            ["ApiKeyAuth:BypassInDevelopment"] = bypassInDev.ToString()
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var middleware = new ApiKeyAuthenticationMiddleware(next, environment, configuration);

        var context = new DefaultHttpContext();
        context.Request.Path = path;
        if (apiKey != null)
            context.Request.Headers["X-Api-Key"] = apiKey;

        return (middleware, context, () => nextCalled);
    }
}

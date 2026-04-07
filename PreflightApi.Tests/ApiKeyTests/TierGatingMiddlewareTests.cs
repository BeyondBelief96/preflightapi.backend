using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;
using Microsoft.Extensions.Options;
using PreflightApi.API.Middleware;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Settings;

namespace PreflightApi.Tests.ApiKeyTests;

public class TierGatingMiddlewareTests
{
    private readonly SubscriptionTierSettings _tierSettings = new()
    {
        Tiers = new Dictionary<string, TierDefinition>
        {
            ["StudentPilot"] = new()
            {
                RateLimitPerMinute = 10, MonthlyQuota = 5000,
                AllowedEndpoints = ["metars", "tafs", "airports", "runways", "communication-frequencies"]
            },
            ["PrivatePilot"] = new()
            {
                RateLimitPerMinute = 60, MonthlyQuota = 150000,
                BlockedEndpoints = ["navlog", "notams", "terminal-procedures", "chart-supplements", "e6b", "navaids"]
            },
            ["CommercialPilot"] = new()
            {
                RateLimitPerMinute = 300, MonthlyQuota = 750000
            }
        }
    };

    // ─── Student Pilot (Whitelist Mode) ─────────────────────────────────────

    [Theory]
    [InlineData("/api/v1/metars")]
    [InlineData("/api/v1/tafs")]
    [InlineData("/api/v1/airports")]
    [InlineData("/api/v1/runways")]
    [InlineData("/api/v1/communication-frequencies")]
    public async Task StudentPilot_ShouldAllow_WhitelistedEndpoints(string path)
    {
        // Arrange
        var (middleware, context, nextCalled) = CreateMiddleware(path, SubscriptionTier.StudentPilot);

        // Act
        await middleware.InvokeAsync(context, Options.Create(_tierSettings));

        // Assert
        nextCalled().Should().BeTrue();
        context.Response.StatusCode.Should().NotBe(403);
    }

    [Theory]
    [InlineData("/api/v1/notams")]
    [InlineData("/api/v1/navlog")]
    [InlineData("/api/v1/e6b")]
    [InlineData("/api/v1/navaids")]
    [InlineData("/api/v1/chart-supplements")]
    [InlineData("/api/v1/terminal-procedures")]
    [InlineData("/api/v1/sigmets")]
    [InlineData("/api/v1/pireps")]
    public async Task StudentPilot_ShouldBlock_NonWhitelistedEndpoints(string path)
    {
        // Arrange
        var (middleware, context, nextCalled) = CreateMiddleware(path, SubscriptionTier.StudentPilot);

        // Act
        await middleware.InvokeAsync(context, Options.Create(_tierSettings));

        // Assert
        nextCalled().Should().BeFalse();
        context.Response.StatusCode.Should().Be(403);
    }

    // ─── Private Pilot (Blocklist Mode) ─────────────────────────────────────

    [Theory]
    [InlineData("/api/v1/metars")]
    [InlineData("/api/v1/airports")]
    [InlineData("/api/v1/sigmets")]
    [InlineData("/api/v1/pireps")]
    [InlineData("/api/v1/airspaces")]
    public async Task PrivatePilot_ShouldAllow_NonBlockedEndpoints(string path)
    {
        // Arrange
        var (middleware, context, nextCalled) = CreateMiddleware(path, SubscriptionTier.PrivatePilot);

        // Act
        await middleware.InvokeAsync(context, Options.Create(_tierSettings));

        // Assert
        nextCalled().Should().BeTrue();
    }

    [Theory]
    [InlineData("/api/v1/navlog")]
    [InlineData("/api/v1/notams")]
    [InlineData("/api/v1/terminal-procedures")]
    [InlineData("/api/v1/chart-supplements")]
    [InlineData("/api/v1/e6b")]
    [InlineData("/api/v1/navaids")]
    public async Task PrivatePilot_ShouldBlock_BlocklistedEndpoints(string path)
    {
        // Arrange
        var (middleware, context, nextCalled) = CreateMiddleware(path, SubscriptionTier.PrivatePilot);

        // Act
        await middleware.InvokeAsync(context, Options.Create(_tierSettings));

        // Assert
        nextCalled().Should().BeFalse();
        context.Response.StatusCode.Should().Be(403);
    }

    // ─── Commercial Pilot (Full Access) ─────────────────────────────────────

    [Theory]
    [InlineData("/api/v1/metars")]
    [InlineData("/api/v1/navlog")]
    [InlineData("/api/v1/notams")]
    [InlineData("/api/v1/e6b")]
    [InlineData("/api/v1/navaids")]
    [InlineData("/api/v1/chart-supplements")]
    [InlineData("/api/v1/terminal-procedures")]
    public async Task CommercialPilot_ShouldAllow_AllEndpoints(string path)
    {
        // Arrange
        var (middleware, context, nextCalled) = CreateMiddleware(path, SubscriptionTier.CommercialPilot);

        // Act
        await middleware.InvokeAsync(context, Options.Create(_tierSettings));

        // Assert
        nextCalled().Should().BeTrue();
    }

    // ─── No API Key (Exempt Paths) ──────────────────────────────────────────

    [Fact]
    public async Task ShouldPassThrough_WhenNoApiKeyInContext()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var middleware = new TierGatingMiddleware(next);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/metars";
        // No ApiKey in Items

        // Act
        await middleware.InvokeAsync(context, Options.Create(_tierSettings));

        // Assert
        nextCalled.Should().BeTrue();
    }

    // ─── Version-Agnostic Matching ──────────────────────────────────────────

    [Theory]
    [InlineData("/api/v1/metars")]
    [InlineData("/api/v2/metars")]
    [InlineData("/api/v10/metars")]
    public async Task ShouldMatchResourceSegment_RegardlessOfVersion(string path)
    {
        // Arrange
        var (middleware, context, nextCalled) = CreateMiddleware(path, SubscriptionTier.StudentPilot);

        // Act
        await middleware.InvokeAsync(context, Options.Create(_tierSettings));

        // Assert
        nextCalled().Should().BeTrue();
    }

    // ─── Error Response Format ──────────────────────────────────────────────

    [Fact]
    public async Task BlockedRequest_ShouldReturnTierRestrictedErrorCode()
    {
        // Arrange
        var (middleware, context, _) = CreateMiddleware("/api/v1/notams", SubscriptionTier.StudentPilot);

        // Act
        await middleware.InvokeAsync(context, Options.Create(_tierSettings));

        // Assert
        context.Response.StatusCode.Should().Be(403);
        context.Response.ContentType.Should().Be("application/json");
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private static (TierGatingMiddleware middleware, HttpContext context, Func<bool> nextCalled) CreateMiddleware(
        string path, SubscriptionTier tier)
    {
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var middleware = new TierGatingMiddleware(next);

        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Items["ApiKey"] = new ApiKey
        {
            Id = Guid.NewGuid(),
            Tier = tier,
            OwnerId = "user_123",
            IsActive = true
        };

        return (middleware, context, () => nextCalled);
    }
}

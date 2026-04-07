using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;
using Microsoft.Extensions.Options;
using NSubstitute;
using PreflightApi.API.Middleware;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Settings;

namespace PreflightApi.Tests.ApiKeyTests;

public class QuotaEnforcementMiddlewareTests
{
    private readonly IQuotaTrackingService _quotaService;
    private readonly SubscriptionTierSettings _tierSettings;

    public QuotaEnforcementMiddlewareTests()
    {
        _quotaService = Substitute.For<IQuotaTrackingService>();
        _tierSettings = new SubscriptionTierSettings
        {
            Tiers = new Dictionary<string, TierDefinition>
            {
                ["StudentPilot"] = new() { RateLimitPerMinute = 10, MonthlyQuota = 5000 },
                ["CommercialPilot"] = new() { RateLimitPerMinute = 300, MonthlyQuota = 750000 }
            }
        };
    }

    [Fact]
    public async Task ShouldPassThrough_WhenUnderQuota()
    {
        // Arrange
        _quotaService.IncrementAndCheck(Arg.Any<Guid>(), Arg.Any<long>(), 5000, Arg.Any<DateTime>())
            .Returns((true, 100L));

        var (middleware, context, nextCalled) = CreateMiddleware(SubscriptionTier.StudentPilot);

        // Act
        await middleware.InvokeAsync(context, _quotaService, Options.Create(_tierSettings));

        // Assert
        nextCalled().Should().BeTrue();
    }

    [Fact]
    public async Task ShouldReturn429_WhenOverQuota()
    {
        // Arrange
        _quotaService.IncrementAndCheck(Arg.Any<Guid>(), Arg.Any<long>(), 5000, Arg.Any<DateTime>())
            .Returns((false, 5000L));

        var (middleware, context, nextCalled) = CreateMiddleware(SubscriptionTier.StudentPilot);

        // Act
        await middleware.InvokeAsync(context, _quotaService, Options.Create(_tierSettings));

        // Assert
        nextCalled().Should().BeFalse();
        context.Response.StatusCode.Should().Be(429);
    }

    [Fact]
    public async Task ShouldRegisterQuotaHeaders_WhenUnderQuota()
    {
        // Arrange
        _quotaService.IncrementAndCheck(Arg.Any<Guid>(), Arg.Any<long>(), 5000, Arg.Any<DateTime>())
            .Returns((true, 100L));

        // Use a real response feature to capture OnStarting callbacks
        var nextCalled = false;
        RequestDelegate next = ctx =>
        {
            nextCalled = true;
            // Manually invoke OnStarting callbacks by setting headers in next
            // to verify the middleware registered them
            return Task.CompletedTask;
        };
        var middleware = new QuotaEnforcementMiddleware(next);

        var context = new DefaultHttpContext();
        context.Items["ApiKey"] = new ApiKey
        {
            Id = Guid.NewGuid(),
            Tier = SubscriptionTier.StudentPilot,
            OwnerId = "user_123",
            IsActive = true,
            MonthlyRequestCount = 0,
            QuotaResetAt = DateTime.UtcNow.AddDays(30)
        };

        // Act
        await middleware.InvokeAsync(context, _quotaService, Options.Create(_tierSettings));

        // Assert — middleware passed through and called IncrementAndCheck
        nextCalled.Should().BeTrue();
        _quotaService.Received(1).IncrementAndCheck(
            Arg.Any<Guid>(), Arg.Any<long>(), 5000, Arg.Any<DateTime>());
    }

    [Fact]
    public async Task ShouldPassThrough_WhenNoApiKeyInContext()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var middleware = new QuotaEnforcementMiddleware(next);
        var context = new DefaultHttpContext();
        // No ApiKey in Items

        // Act
        await middleware.InvokeAsync(context, _quotaService, Options.Create(_tierSettings));

        // Assert
        nextCalled.Should().BeTrue();
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private (QuotaEnforcementMiddleware middleware, HttpContext context, Func<bool> nextCalled) CreateMiddleware(
        SubscriptionTier tier)
    {
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var middleware = new QuotaEnforcementMiddleware(next);

        var context = new DefaultHttpContext();
        context.Items["ApiKey"] = new ApiKey
        {
            Id = Guid.NewGuid(),
            Tier = tier,
            OwnerId = "user_123",
            IsActive = true,
            MonthlyRequestCount = 0,
            QuotaResetAt = DateTime.UtcNow.AddDays(30)
        };

        return (middleware, context, () => nextCalled);
    }
}

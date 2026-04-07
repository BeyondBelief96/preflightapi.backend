using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PreflightApi.Infrastructure.Services;

namespace PreflightApi.Tests.ApiKeyTests;

public class QuotaTrackingServiceTests
{
    private readonly QuotaTrackingService _sut;

    public QuotaTrackingServiceTests()
    {
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        _sut = new QuotaTrackingService(scopeFactory, Substitute.For<ILogger<QuotaTrackingService>>());
    }

    // ─── IncrementAndCheck ──────────────────────────────────────────────────

    [Fact]
    public void IncrementAndCheck_ShouldAllowRequest_WhenUnderLimit()
    {
        // Arrange
        var keyId = Guid.NewGuid();
        var resetAt = DateTime.UtcNow.AddDays(30);

        // Act
        var (allowed, count) = _sut.IncrementAndCheck(keyId, dbCount: 0, monthlyLimit: 5000, quotaResetAt: resetAt);

        // Assert
        allowed.Should().BeTrue();
        count.Should().Be(1);
    }

    [Fact]
    public void IncrementAndCheck_ShouldDenyRequest_WhenAtLimit()
    {
        // Arrange
        var keyId = Guid.NewGuid();
        var resetAt = DateTime.UtcNow.AddDays(30);

        // Fill up to the limit
        for (int i = 0; i < 100; i++)
            _sut.IncrementAndCheck(keyId, dbCount: 0, monthlyLimit: 100, quotaResetAt: resetAt);

        // Act — 101st request
        var (allowed, count) = _sut.IncrementAndCheck(keyId, dbCount: 0, monthlyLimit: 100, quotaResetAt: resetAt);

        // Assert
        allowed.Should().BeFalse();
        count.Should().Be(100); // Count should not increment past limit
    }

    [Fact]
    public void IncrementAndCheck_ShouldStartFromDbCount()
    {
        // Arrange
        var keyId = Guid.NewGuid();
        var resetAt = DateTime.UtcNow.AddDays(30);

        // Act — first call with dbCount of 4999 on a limit of 5000
        var (allowed1, count1) = _sut.IncrementAndCheck(keyId, dbCount: 4999, monthlyLimit: 5000, quotaResetAt: resetAt);
        var (allowed2, count2) = _sut.IncrementAndCheck(keyId, dbCount: 4999, monthlyLimit: 5000, quotaResetAt: resetAt);

        // Assert
        allowed1.Should().BeTrue();
        count1.Should().Be(5000);
        allowed2.Should().BeFalse();
        count2.Should().Be(5000);
    }

    [Fact]
    public void IncrementAndCheck_ShouldResetCounter_WhenQuotaPeriodExpired()
    {
        // Arrange
        var keyId = Guid.NewGuid();
        var expiredResetAt = DateTime.UtcNow.AddHours(-1); // Already expired

        // Fill up some usage
        _sut.IncrementAndCheck(keyId, dbCount: 4500, monthlyLimit: 5000, quotaResetAt: expiredResetAt);

        // Act — next call should detect expired quota and reset
        var (allowed, count) = _sut.IncrementAndCheck(keyId, dbCount: 4500, monthlyLimit: 5000, quotaResetAt: expiredResetAt);

        // Assert
        allowed.Should().BeTrue();
        count.Should().BeLessThanOrEqualTo(2); // Reset to 0 then incremented
    }

    // ─── GetCurrentCount ────────────────────────────────────────────────────

    [Fact]
    public void GetCurrentCount_ShouldReturnZero_WhenKeyNotTracked()
    {
        // Act
        var count = _sut.GetCurrentCount(Guid.NewGuid());

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public void GetCurrentCount_ShouldReturnCurrentCount()
    {
        // Arrange
        var keyId = Guid.NewGuid();
        var resetAt = DateTime.UtcNow.AddDays(30);
        _sut.IncrementAndCheck(keyId, dbCount: 0, monthlyLimit: 5000, quotaResetAt: resetAt);
        _sut.IncrementAndCheck(keyId, dbCount: 0, monthlyLimit: 5000, quotaResetAt: resetAt);
        _sut.IncrementAndCheck(keyId, dbCount: 0, monthlyLimit: 5000, quotaResetAt: resetAt);

        // Act
        var count = _sut.GetCurrentCount(keyId);

        // Assert
        count.Should().Be(3);
    }

    // ─── Thread Safety ──────────────────────────────────────────────────────

    [Fact]
    public void IncrementAndCheck_ShouldBeThreadSafe()
    {
        // Arrange
        var keyId = Guid.NewGuid();
        var resetAt = DateTime.UtcNow.AddDays(30);
        const int threadCount = 100;
        var allowedCount = 0;

        // Act — concurrent increments up to limit of 50
        Parallel.For(0, threadCount, _ =>
        {
            var (allowed, _) = _sut.IncrementAndCheck(keyId, dbCount: 0, monthlyLimit: 50, quotaResetAt: resetAt);
            if (allowed)
                Interlocked.Increment(ref allowedCount);
        });

        // Assert — exactly 50 should be allowed
        allowedCount.Should().Be(50);
        _sut.GetCurrentCount(keyId).Should().Be(50);
    }
}

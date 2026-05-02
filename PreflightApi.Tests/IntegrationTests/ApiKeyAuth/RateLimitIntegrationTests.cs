using System.Net;
using FluentAssertions;
using PreflightApi.Domain.Enums;
using Xunit;

namespace PreflightApi.Tests.IntegrationTests.ApiKeyAuth;

[Collection("Integration")]
public class RateLimitIntegrationTests : IClassFixture<ApiKeyTestFixture>
{
    private readonly ApiKeyTestFixture _fx;

    public RateLimitIntegrationTests(ApiKeyTestFixture fx) => _fx = fx;

    // StudentPilot rate limit per appsettings.json: 10 / minute (fixed window).

    [Fact]
    public async Task StudentTier_BurstingPastLimit_Returns429()
    {
        var raw = await _fx.SeedKeyAsync("user_rate_burst", SubscriptionTier.StudentPilot);
        var client = _fx.CreateClientWithKey(raw);

        // Fire 10 requests serially — should all succeed under the 10/min limit
        for (int i = 0; i < 10; i++)
        {
            var ok = await client.GetAsync("/api/v1/metars/KDFW");
            ok.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests, $"request #{i + 1} should be under the limit");
        }

        // 11th request inside the same minute window → 429
        var rejected = await client.GetAsync("/api/v1/metars/KDFW");

        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        rejected.Headers.Should().Contain(h => h.Key == "Retry-After");
        rejected.Headers.Should().Contain(h => h.Key == "X-RateLimit-Limit");
    }

    [Fact]
    public async Task DifferentKeys_HaveSeparateRateLimitBuckets()
    {
        var rawA = await _fx.SeedKeyAsync("user_rate_a", SubscriptionTier.StudentPilot);
        var rawB = await _fx.SeedKeyAsync("user_rate_b", SubscriptionTier.StudentPilot);
        var clientA = _fx.CreateClientWithKey(rawA);
        var clientB = _fx.CreateClientWithKey(rawB);

        // Exhaust A's bucket
        for (int i = 0; i < 11; i++)
            await clientA.GetAsync("/api/v1/metars/KDFW");

        // B should still be unaffected — separate bucket per ApiKey.Id partition
        var bResponse = await clientB.GetAsync("/api/v1/metars/KDFW");

        bResponse.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
    }
}

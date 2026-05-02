using System.Net;
using FluentAssertions;
using PreflightApi.Domain.Enums;
using Xunit;

namespace PreflightApi.Tests.IntegrationTests.ApiKeyAuth;

[Collection("Integration")]
public class QuotaIntegrationTests : IClassFixture<ApiKeyTestFixture>
{
    private readonly ApiKeyTestFixture _fx;

    public QuotaIntegrationTests(ApiKeyTestFixture fx) => _fx = fx;

    [Fact]
    public async Task SuccessfulRequest_IncludesQuotaHeaders()
    {
        var raw = await _fx.SeedKeyAsync("user_quota_headers", SubscriptionTier.StudentPilot);
        var client = _fx.CreateClientWithKey(raw);

        var response = await client.GetAsync("/api/v1/metars/KDFW");

        response.Headers.GetValues("X-Quota-Limit").Should().ContainSingle().Which.Should().Be("5000");
        response.Headers.GetValues("X-Quota-Remaining").Should().ContainSingle().Which.Should().Be("4999");
        response.Headers.Should().Contain(h => h.Key == "X-Quota-Resets-At");
    }

    [Fact]
    public async Task RepeatedRequests_DecrementRemainingHeader()
    {
        var raw = await _fx.SeedKeyAsync("user_quota_decrement", SubscriptionTier.StudentPilot);
        var client = _fx.CreateClientWithKey(raw);

        var first = await client.GetAsync("/api/v1/metars/KDFW");
        var second = await client.GetAsync("/api/v1/metars/KDFW");
        var third = await client.GetAsync("/api/v1/metars/KAUS");

        first.Headers.GetValues("X-Quota-Remaining").Should().ContainSingle().Which.Should().Be("4999");
        second.Headers.GetValues("X-Quota-Remaining").Should().ContainSingle().Which.Should().Be("4998");
        third.Headers.GetValues("X-Quota-Remaining").Should().ContainSingle().Which.Should().Be("4997");
    }

    [Fact]
    public async Task RequestAtQuotaLimit_Returns429()
    {
        // Seed near-limit so we don't have to fire 5000 requests.
        var raw = await _fx.SeedKeyAsync(
            "user_quota_exhausted",
            SubscriptionTier.StudentPilot,
            monthlyRequestCount: 5000);
        var client = _fx.CreateClientWithKey(raw);

        var response = await client.GetAsync("/api/v1/metars/KDFW");

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("QUOTA_EXCEEDED");
    }
}

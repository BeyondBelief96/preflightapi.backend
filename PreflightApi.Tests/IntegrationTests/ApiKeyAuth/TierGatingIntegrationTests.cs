using System.Net;
using FluentAssertions;
using PreflightApi.Domain.Enums;
using Xunit;

namespace PreflightApi.Tests.IntegrationTests.ApiKeyAuth;

[Collection("Integration")]
public class TierGatingIntegrationTests : IClassFixture<ApiKeyTestFixture>
{
    private readonly ApiKeyTestFixture _fx;

    public TierGatingIntegrationTests(ApiKeyTestFixture fx) => _fx = fx;

    // Allowlist for StudentPilot per appsettings.json:
    //   metars, tafs, airports, runways, communication-frequencies
    // Anything else → 403 TIER_RESTRICTED.

    [Fact]
    public async Task StudentTier_AllowlistedEndpoint_PassesGating()
    {
        var raw = await _fx.SeedKeyAsync("user_tier_student_ok", SubscriptionTier.StudentPilot);
        var client = _fx.CreateClientWithKey(raw);

        var response = await client.GetAsync("/api/v1/metars/KDFW");

        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task StudentTier_NonAllowlistedEndpoint_ReturnsForbidden()
    {
        var raw = await _fx.SeedKeyAsync("user_tier_student_blocked", SubscriptionTier.StudentPilot);
        var client = _fx.CreateClientWithKey(raw);

        // /pireps is NOT in the StudentPilot allowlist
        var response = await client.GetAsync("/api/v1/pireps");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("TIER_RESTRICTED");
    }

    [Fact]
    public async Task StudentTier_NotamsEndpoint_ReturnsForbidden()
    {
        var raw = await _fx.SeedKeyAsync("user_tier_student_notams", SubscriptionTier.StudentPilot);
        var client = _fx.CreateClientWithKey(raw);

        var response = await client.GetAsync("/api/v1/notams");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // PrivatePilot uses a blocklist:
    //   navlog, notams, terminal-procedures, chart-supplements, e6b, navaids
    // Everything else → allowed.

    [Fact]
    public async Task PrivateTier_NonBlockedEndpoint_PassesGating()
    {
        var raw = await _fx.SeedKeyAsync("user_tier_private_ok", SubscriptionTier.PrivatePilot);
        var client = _fx.CreateClientWithKey(raw);

        var response = await client.GetAsync("/api/v1/metars/KDFW");

        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PrivateTier_BlockedEndpoint_ReturnsForbidden()
    {
        var raw = await _fx.SeedKeyAsync("user_tier_private_blocked", SubscriptionTier.PrivatePilot);
        var client = _fx.CreateClientWithKey(raw);

        var response = await client.GetAsync("/api/v1/notams");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CommercialTier_AnyEndpoint_PassesGating()
    {
        var raw = await _fx.SeedKeyAsync("user_tier_commercial", SubscriptionTier.CommercialPilot);
        var client = _fx.CreateClientWithKey(raw);

        // Endpoints that PrivatePilot would be blocked from
        var notams = await client.GetAsync("/api/v1/notams");
        var navaids = await client.GetAsync("/api/v1/navaids");

        notams.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        navaids.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}

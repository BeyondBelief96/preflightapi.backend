using System.Net;
using FluentAssertions;
using PreflightApi.Domain.Enums;
using Xunit;

namespace PreflightApi.Tests.IntegrationTests.ApiKeyAuth;

[Collection("Integration")]
public class ApiKeyAuthIntegrationTests : IClassFixture<ApiKeyTestFixture>
{
    private readonly ApiKeyTestFixture _fx;

    public ApiKeyAuthIntegrationTests(ApiKeyTestFixture fx) => _fx = fx;

    [Fact]
    public async Task NoApiKeyHeader_ReturnsUnauthorized()
    {
        var client = _fx.CreateClient();

        var response = await client.GetAsync("/api/v1/metars/KDFW");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InvalidApiKey_ReturnsUnauthorized()
    {
        var client = _fx.CreateClientWithKey("pfa_sk_thisDoesNotExistInDb");

        var response = await client.GetAsync("/api/v1/metars/KDFW");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task NonPrefixedKey_ReturnsUnauthorized()
    {
        var client = _fx.CreateClientWithKey("legacyAPIMkey1234567890abcdef");

        var response = await client.GetAsync("/api/v1/metars/KDFW");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RevokedKey_ReturnsUnauthorized()
    {
        var raw = await _fx.SeedKeyAsync(
            ownerId: "user_revoked",
            tier: SubscriptionTier.StudentPilot,
            isActive: false,
            revokedAt: DateTime.UtcNow);
        var client = _fx.CreateClientWithKey(raw);

        var response = await client.GetAsync("/api/v1/metars/KDFW");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExpiredKey_ReturnsUnauthorized()
    {
        var raw = await _fx.SeedKeyAsync(
            ownerId: "user_expired",
            tier: SubscriptionTier.StudentPilot,
            expiresAt: DateTime.UtcNow.AddDays(-1));
        var client = _fx.CreateClientWithKey(raw);

        var response = await client.GetAsync("/api/v1/metars/KDFW");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ValidKey_PassesAuth()
    {
        var raw = await _fx.SeedKeyAsync("user_auth_ok", SubscriptionTier.StudentPilot);
        var client = _fx.CreateClientWithKey(raw);

        var response = await client.GetAsync("/api/v1/metars/KDFW");

        // 404 (no METAR for KDFW in the empty test DB) is correct here — auth passed,
        // request reached the controller. We're testing the auth layer, not data.
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task HealthEndpoint_BypassesAuth()
    {
        var client = _fx.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task StripeWebhook_BypassesAuth()
    {
        var client = _fx.CreateClient();

        // No body, no signature — webhook controller should reject for missing
        // signature header (400), not for missing API key (401).
        var response = await client.PostAsync("/webhooks/stripe", new StringContent(""));

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }
}

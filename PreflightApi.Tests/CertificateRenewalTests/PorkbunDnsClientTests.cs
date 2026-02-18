using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using RichardSzalay.MockHttp;
using PreflightApi.Infrastructure.Services.CertificateRenewal;
using PreflightApi.Infrastructure.Settings;
using Xunit;

namespace PreflightApi.Tests.CertificateRenewalTests;

public class PorkbunDnsClientTests
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly PorkbunDnsClient _client;

    private const string RootDomain = "preflightapi.io";
    private const string Subdomain = "_acme-challenge.api";
    private const string TxtContent = "test-challenge-token";

    public PorkbunDnsClientTests()
    {
        _mockHttp = new MockHttpMessageHandler();

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient("Porkbun").Returns(_mockHttp.ToHttpClient());

        var settings = Options.Create(new PorkbunSettings
        {
            ApiKey = "pk1_test",
            SecretApiKey = "sk1_test"
        });

        var logger = Substitute.For<ILogger<PorkbunDnsClient>>();

        _client = new PorkbunDnsClient(httpClientFactory, settings, logger);
    }

    [Fact]
    public async Task CreateTxtRecordAsync_SuccessfulResponse_DoesNotThrow()
    {
        // Arrange
        _mockHttp
            .When(HttpMethod.Post, $"https://api.porkbun.com/api/json/v3/dns/create/{RootDomain}")
            .Respond(HttpStatusCode.OK, "application/json", """{"status":"SUCCESS"}""");

        // Act
        var act = () => _client.CreateTxtRecordAsync(RootDomain, Subdomain, TxtContent);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CreateTxtRecordAsync_FailedResponse_ThrowsHttpRequestException()
    {
        // Arrange
        _mockHttp
            .When(HttpMethod.Post, $"https://api.porkbun.com/api/json/v3/dns/create/{RootDomain}")
            .Respond(HttpStatusCode.BadRequest, "application/json", """{"status":"ERROR","message":"Invalid domain"}""");

        // Act
        var act = () => _client.CreateTxtRecordAsync(RootDomain, Subdomain, TxtContent);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*Porkbun DNS create failed*");
    }

    [Fact]
    public async Task CreateTxtRecordAsync_SendsCorrectPayload()
    {
        // Arrange
        string? capturedBody = null;
        _mockHttp
            .When(HttpMethod.Post, $"https://api.porkbun.com/api/json/v3/dns/create/{RootDomain}")
            .With(req =>
            {
                capturedBody = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return true;
            })
            .Respond(HttpStatusCode.OK, "application/json", """{"status":"SUCCESS"}""");

        // Act
        await _client.CreateTxtRecordAsync(RootDomain, Subdomain, TxtContent);

        // Assert
        capturedBody.Should().NotBeNull();
        capturedBody.Should().Contain("\"apikey\":\"pk1_test\"");
        capturedBody.Should().Contain("\"secretapikey\":\"sk1_test\"");
        capturedBody.Should().Contain("\"type\":\"TXT\"");
        capturedBody.Should().Contain($"\"name\":\"{Subdomain}\"");
        capturedBody.Should().Contain($"\"content\":\"{TxtContent}\"");
    }

    [Fact]
    public async Task DeleteTxtRecordAsync_SuccessfulResponse_DoesNotThrow()
    {
        // Arrange
        _mockHttp
            .When(HttpMethod.Post, $"https://api.porkbun.com/api/json/v3/dns/deleteByNameType/{RootDomain}/TXT/{Subdomain}")
            .Respond(HttpStatusCode.OK, "application/json", """{"status":"SUCCESS"}""");

        // Act
        var act = () => _client.DeleteTxtRecordAsync(RootDomain, Subdomain);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteTxtRecordAsync_FailedResponse_DoesNotThrow()
    {
        // Arrange — delete is best-effort, should not throw
        _mockHttp
            .When(HttpMethod.Post, $"https://api.porkbun.com/api/json/v3/dns/deleteByNameType/{RootDomain}/TXT/{Subdomain}")
            .Respond(HttpStatusCode.BadRequest, "application/json", """{"status":"ERROR","message":"Record not found"}""");

        // Act
        var act = () => _client.DeleteTxtRecordAsync(RootDomain, Subdomain);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteTxtRecordAsync_SendsAuthPayload()
    {
        // Arrange
        string? capturedBody = null;
        _mockHttp
            .When(HttpMethod.Post, $"https://api.porkbun.com/api/json/v3/dns/deleteByNameType/{RootDomain}/TXT/{Subdomain}")
            .With(req =>
            {
                capturedBody = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return true;
            })
            .Respond(HttpStatusCode.OK, "application/json", """{"status":"SUCCESS"}""");

        // Act
        await _client.DeleteTxtRecordAsync(RootDomain, Subdomain);

        // Assert
        capturedBody.Should().NotBeNull();
        capturedBody.Should().Contain("\"apikey\":\"pk1_test\"");
        capturedBody.Should().Contain("\"secretapikey\":\"sk1_test\"");
    }
}

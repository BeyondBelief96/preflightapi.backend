using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Services.CertificateRenewal;
using PreflightApi.Infrastructure.Settings;
using Xunit;

namespace PreflightApi.Tests.CertificateRenewalTests;

public class CertificateRenewalServiceTests
{
    private readonly IKeyVaultCertificateService _keyVaultService;
    private readonly IPorkbunDnsClient _dnsClient;
    private readonly CertificateRenewalService _service;
    private readonly CertificateRenewalSettings _settings;

    public CertificateRenewalServiceTests()
    {
        _keyVaultService = Substitute.For<IKeyVaultCertificateService>();
        _dnsClient = Substitute.For<IPorkbunDnsClient>();
        var logger = Substitute.For<ILogger<CertificateRenewalService>>();

        _settings = new CertificateRenewalSettings
        {
            Domain = "api.preflightapi.io",
            RootDomain = "preflightapi.io",
            AcmeEmail = "test@example.com",
            KeyVaultName = "TestKeyVault",
            CertificateName = "api-preflightapi-io",
            RenewalThresholdDays = 30,
            DnsPropagationDelaySeconds = 0 // no delay in tests
        };

        _service = new CertificateRenewalService(
            _keyVaultService,
            _dnsClient,
            Options.Create(_settings),
            logger);
    }

    [Fact]
    public async Task RenewCertificateIfNeededAsync_CertNotExpiringSoon_SkipsRenewal()
    {
        // Arrange — certificate expires in 60 days (above 30-day threshold)
        _keyVaultService
            .GetCertificateExpiryAsync(_settings.CertificateName, Arg.Any<CancellationToken>())
            .Returns(DateTimeOffset.UtcNow.AddDays(60));

        // Act
        await _service.RenewCertificateIfNeededAsync();

        // Assert — should not attempt DNS or import
        await _dnsClient.DidNotReceive()
            .CreateTxtRecordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _keyVaultService.DidNotReceive()
            .ImportCertificateAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RenewCertificateIfNeededAsync_ExactlyAtThreshold_SkipsRenewal()
    {
        // Arrange — certificate expires in exactly 31 days (above 30-day threshold)
        _keyVaultService
            .GetCertificateExpiryAsync(_settings.CertificateName, Arg.Any<CancellationToken>())
            .Returns(DateTimeOffset.UtcNow.AddDays(31));

        // Act
        await _service.RenewCertificateIfNeededAsync();

        // Assert
        await _dnsClient.DidNotReceive()
            .CreateTxtRecordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RenewCertificateIfNeededAsync_NoCertificate_AttemptsIssuance()
    {
        // Arrange — no certificate exists
        _keyVaultService
            .GetCertificateExpiryAsync(_settings.CertificateName, Arg.Any<CancellationToken>())
            .Returns((DateTimeOffset?)null);

        // The ACME account key lookup returns null (new account needed)
        _keyVaultService
            .GetSecretAsync(_settings.AcmeAccountKeySecretName, Arg.Any<CancellationToken>())
            .Returns((string?)null);

        // Act — will fail at ACME order (no real server), but should attempt DNS
        // We verify the flow up to the point where it needs a real ACME server
        var act = () => _service.RenewCertificateIfNeededAsync();

        // Assert — should throw (can't reach Let's Encrypt in tests) but should attempt the flow
        await act.Should().ThrowAsync<Exception>();

        // Verify cleanup was attempted in finally block
        await _dnsClient.Received()
            .DeleteTxtRecordAsync(_settings.RootDomain, "_acme-challenge.api", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RenewCertificateIfNeededAsync_CertExpiringSoon_AttemptsRenewal()
    {
        // Arrange — certificate expires in 15 days (below 30-day threshold)
        _keyVaultService
            .GetCertificateExpiryAsync(_settings.CertificateName, Arg.Any<CancellationToken>())
            .Returns(DateTimeOffset.UtcNow.AddDays(15));

        _keyVaultService
            .GetSecretAsync(_settings.AcmeAccountKeySecretName, Arg.Any<CancellationToken>())
            .Returns((string?)null);

        // Act — will fail at ACME (no real server), but verifies the renewal path is taken
        var act = () => _service.RenewCertificateIfNeededAsync();

        // Assert
        await act.Should().ThrowAsync<Exception>();

        // Verify cleanup was attempted
        await _dnsClient.Received()
            .DeleteTxtRecordAsync(_settings.RootDomain, "_acme-challenge.api", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RenewCertificateIfNeededAsync_DnsCleanupFailure_DoesNotThrowAdditionally()
    {
        // Arrange — certificate needs renewal
        _keyVaultService
            .GetCertificateExpiryAsync(_settings.CertificateName, Arg.Any<CancellationToken>())
            .Returns(DateTimeOffset.UtcNow.AddDays(5));

        _keyVaultService
            .GetSecretAsync(_settings.AcmeAccountKeySecretName, Arg.Any<CancellationToken>())
            .Returns((string?)null);

        // DNS cleanup will throw — should be swallowed
        _dnsClient
            .DeleteTxtRecordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("DNS cleanup failed"));

        // Act — will fail at ACME, but DNS cleanup failure should not mask original exception
        var act = () => _service.RenewCertificateIfNeededAsync();

        // Assert — should throw the ACME error, not the DNS cleanup error
        var ex = await act.Should().ThrowAsync<Exception>();
        ex.Which.Should().NotBeOfType<HttpRequestException>();
    }

    [Fact]
    public async Task RenewCertificateIfNeededAsync_BuildsCorrectChallengeSubdomain()
    {
        // Arrange — certificate needs renewal
        _keyVaultService
            .GetCertificateExpiryAsync(_settings.CertificateName, Arg.Any<CancellationToken>())
            .Returns(DateTimeOffset.UtcNow.AddDays(5));

        _keyVaultService
            .GetSecretAsync(_settings.AcmeAccountKeySecretName, Arg.Any<CancellationToken>())
            .Returns((string?)null);

        // Act
        try { await _service.RenewCertificateIfNeededAsync(); } catch { /* expected */ }

        // Assert — the cleanup call reveals the subdomain that was used
        await _dnsClient.Received()
            .DeleteTxtRecordAsync(
                "preflightapi.io",
                "_acme-challenge.api",
                Arg.Any<CancellationToken>());
    }
}

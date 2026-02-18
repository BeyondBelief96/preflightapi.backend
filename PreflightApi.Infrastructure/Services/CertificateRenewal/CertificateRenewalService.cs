using System.Security.Cryptography.X509Certificates;
using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Settings;

namespace PreflightApi.Infrastructure.Services.CertificateRenewal;

public class CertificateRenewalService : ICertificateRenewalService
{
    private readonly IKeyVaultCertificateService _keyVaultService;
    private readonly IPorkbunDnsClient _dnsClient;
    private readonly CertificateRenewalSettings _settings;
    private readonly ILogger<CertificateRenewalService> _logger;

    public CertificateRenewalService(
        IKeyVaultCertificateService keyVaultService,
        IPorkbunDnsClient dnsClient,
        IOptions<CertificateRenewalSettings> settings,
        ILogger<CertificateRenewalService> logger)
    {
        _keyVaultService = keyVaultService;
        _dnsClient = dnsClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task RenewCertificateIfNeededAsync(CancellationToken ct = default)
    {
        // Check certificate expiry
        var expiry = await _keyVaultService.GetCertificateExpiryAsync(_settings.CertificateName, ct);

        if (expiry.HasValue)
        {
            var daysUntilExpiry = (expiry.Value - DateTimeOffset.UtcNow).TotalDays;
            _logger.LogInformation(
                "Certificate '{CertName}' expires on {Expiry:O} ({Days:F0} days remaining)",
                _settings.CertificateName, expiry.Value, daysUntilExpiry);

            if (daysUntilExpiry > _settings.RenewalThresholdDays)
            {
                _logger.LogInformation("Certificate does not need renewal (threshold: {Threshold} days)", _settings.RenewalThresholdDays);
                return;
            }

            _logger.LogInformation("Certificate is within renewal threshold — proceeding with renewal");
        }
        else
        {
            _logger.LogInformation("No existing certificate found — proceeding with initial issuance");
        }

        // Build the full ACME challenge subdomain: _acme-challenge.api for api.preflightapi.io
        var domainPrefix = _settings.Domain.Replace($".{_settings.RootDomain}", "");
        var challengeSubdomain = $"{_settings.AcmeChallengeSubdomain}.{domainPrefix}";

        try
        {
            // Load or create ACME account
            var acme = await GetOrCreateAcmeContextAsync(ct);

            // Create certificate order
            _logger.LogInformation("Creating ACME order for {Domain}", _settings.Domain);
            var order = await acme.NewOrder(new[] { _settings.Domain });

            // Get DNS-01 challenge
            var authz = (await order.Authorizations()).First();
            var challenge = await authz.Dns();
            var txtValue = acme.AccountKey.DnsTxt(challenge.Token);

            _logger.LogInformation("DNS-01 challenge token generated, creating TXT record");

            // Create DNS TXT record
            await _dnsClient.CreateTxtRecordAsync(_settings.RootDomain, challengeSubdomain, txtValue, ct);

            // Wait for DNS propagation
            _logger.LogInformation("Waiting {Seconds}s for DNS propagation", _settings.DnsPropagationDelaySeconds);
            await Task.Delay(TimeSpan.FromSeconds(_settings.DnsPropagationDelaySeconds), ct);

            // Validate the challenge (poll up to 10 times with 5s intervals)
            await challenge.Validate();
            await WaitForChallengeValidationAsync(authz, ct);

            // Generate certificate
            _logger.LogInformation("Challenge validated, generating certificate");
            var privateKey = KeyFactory.NewKey(Certes.KeyAlgorithm.ES256);
            var cert = await order.Generate(new CsrInfo { CommonName = _settings.Domain }, privateKey);

            // Export as PFX
            var pfxBuilder = cert.ToPfx(privateKey);
            var pfxBytes = pfxBuilder.Build(_settings.Domain, string.Empty);

            // Import to Key Vault
            await _keyVaultService.ImportCertificateAsync(_settings.CertificateName, pfxBytes, ct);
            _logger.LogInformation("Certificate renewal completed successfully for {Domain}", _settings.Domain);
        }
        finally
        {
            // Clean up DNS TXT record (best-effort)
            try
            {
                await _dnsClient.DeleteTxtRecordAsync(_settings.RootDomain, challengeSubdomain, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up DNS TXT record (non-critical)");
            }
        }
    }

    private async Task<AcmeContext> GetOrCreateAcmeContextAsync(CancellationToken ct)
    {
        var acmeServer = _settings.UseStagingEnvironment
            ? WellKnownServers.LetsEncryptStagingV2
            : WellKnownServers.LetsEncryptV2;

        var existingKey = await _keyVaultService.GetSecretAsync(_settings.AcmeAccountKeySecretName, ct);

        if (existingKey is not null)
        {
            _logger.LogInformation("Loading existing ACME account from Key Vault");
            var accountKey = KeyFactory.FromPem(existingKey);
            return new AcmeContext(acmeServer, accountKey);
        }

        _logger.LogInformation("Creating new ACME account for {Email}", _settings.AcmeEmail);
        var acme = new AcmeContext(acmeServer);
        await acme.NewAccount(_settings.AcmeEmail, termsOfServiceAgreed: true);

        // Persist the account key
        var pem = acme.AccountKey.ToPem();
        await _keyVaultService.SetSecretAsync(_settings.AcmeAccountKeySecretName, pem, ct);

        return acme;
    }

    private async Task WaitForChallengeValidationAsync(IAuthorizationContext authz, CancellationToken ct)
    {
        const int maxAttempts = 10;
        const int delaySeconds = 5;

        for (var i = 0; i < maxAttempts; i++)
        {
            var authzResource = await authz.Resource();

            if (authzResource.Status == AuthorizationStatus.Valid)
            {
                _logger.LogInformation("ACME challenge validated on attempt {Attempt}", i + 1);
                return;
            }

            if (authzResource.Status == AuthorizationStatus.Invalid)
            {
                var challengeDetails = authzResource.Challenges?.FirstOrDefault();
                var errorDetail = challengeDetails?.Error?.Detail ?? "Unknown error";
                throw new InvalidOperationException($"ACME challenge validation failed: {errorDetail}");
            }

            _logger.LogInformation("Challenge status: {Status}, polling again ({Attempt}/{Max})",
                authzResource.Status, i + 1, maxAttempts);

            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), ct);
        }

        throw new TimeoutException($"ACME challenge validation did not complete after {maxAttempts} attempts");
    }
}

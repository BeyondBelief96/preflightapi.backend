using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Settings;

namespace PreflightApi.Infrastructure.Services.CertificateRenewal;

public class KeyVaultCertificateService : IKeyVaultCertificateService
{
    private readonly Lazy<CertificateClient> _certificateClient;
    private readonly Lazy<SecretClient> _secretClient;
    private readonly ILogger<KeyVaultCertificateService> _logger;

    public KeyVaultCertificateService(
        IOptions<CertificateRenewalSettings> settings,
        ILogger<KeyVaultCertificateService> logger)
    {
        _logger = logger;

        _certificateClient = new Lazy<CertificateClient>(() =>
        {
            var vaultUri = new Uri($"https://{settings.Value.KeyVaultName}.vault.azure.net/");
            return new CertificateClient(vaultUri, new DefaultAzureCredential());
        });

        _secretClient = new Lazy<SecretClient>(() =>
        {
            var vaultUri = new Uri($"https://{settings.Value.KeyVaultName}.vault.azure.net/");
            return new SecretClient(vaultUri, new DefaultAzureCredential());
        });
    }

    public async Task<DateTimeOffset?> GetCertificateExpiryAsync(string certificateName, CancellationToken ct = default)
    {
        try
        {
            var certificate = await _certificateClient.Value.GetCertificateAsync(certificateName, ct);
            return certificate.Value.Properties.ExpiresOn;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Certificate '{CertificateName}' not found in Key Vault", certificateName);
            return null;
        }
    }

    public async Task ImportCertificateAsync(string certificateName, byte[] pfxBytes, CancellationToken ct = default)
    {
        var importOptions = new ImportCertificateOptions(certificateName, pfxBytes);
        await _certificateClient.Value.ImportCertificateAsync(importOptions, ct);
        _logger.LogInformation("Certificate '{CertificateName}' imported to Key Vault", certificateName);
    }

    public async Task<string?> GetSecretAsync(string secretName, CancellationToken ct = default)
    {
        try
        {
            var secret = await _secretClient.Value.GetSecretAsync(secretName, cancellationToken: ct);
            return secret.Value.Value;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogInformation("Secret '{SecretName}' not found in Key Vault", secretName);
            return null;
        }
    }

    public async Task SetSecretAsync(string secretName, string value, CancellationToken ct = default)
    {
        await _secretClient.Value.SetSecretAsync(secretName, value, ct);
        _logger.LogInformation("Secret '{SecretName}' saved to Key Vault", secretName);
    }
}

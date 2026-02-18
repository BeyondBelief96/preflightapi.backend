namespace PreflightApi.Infrastructure.Interfaces;

public interface IKeyVaultCertificateService
{
    Task<DateTimeOffset?> GetCertificateExpiryAsync(string certificateName, CancellationToken ct = default);
    Task ImportCertificateAsync(string certificateName, byte[] pfxBytes, CancellationToken ct = default);
    Task<string?> GetSecretAsync(string secretName, CancellationToken ct = default);
    Task SetSecretAsync(string secretName, string value, CancellationToken ct = default);
}

namespace PreflightApi.Infrastructure.Interfaces;

public interface ICertificateRenewalService
{
    Task RenewCertificateIfNeededAsync(CancellationToken ct = default);
}

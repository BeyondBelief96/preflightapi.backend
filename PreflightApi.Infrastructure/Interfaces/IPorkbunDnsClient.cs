namespace PreflightApi.Infrastructure.Interfaces;

public interface IPorkbunDnsClient
{
    Task CreateTxtRecordAsync(string rootDomain, string subdomain, string content, CancellationToken ct = default);
    Task DeleteTxtRecordAsync(string rootDomain, string subdomain, CancellationToken ct = default);
}

namespace PreflightApi.Infrastructure.Settings;

public class CertificateRenewalSettings
{
    public required string Domain { get; init; }
    public required string RootDomain { get; init; }
    public string AcmeChallengeSubdomain { get; init; } = "_acme-challenge";
    public required string AcmeEmail { get; init; }
    public required string KeyVaultName { get; init; }
    public required string CertificateName { get; init; }
    public string AcmeAccountKeySecretName { get; init; } = "acme-account-key";
    public int RenewalThresholdDays { get; init; } = 30;
    public bool UseStagingEnvironment { get; init; }
    public int DnsPropagationDelaySeconds { get; init; } = 60;
}

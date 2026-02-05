using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;

namespace PreflightApi.API.Configuration;

/// <summary>
/// Custom Key Vault secret manager that maps Key Vault secret names to configuration keys.
/// This allows environment-specific secrets (e.g., preflightapi-faa-nms-api-client-id-staging vs preflightapi-faa-nms-api-client-id-prd)
/// to be mapped to the same configuration key (e.g., NmsSettings:ClientId).
/// </summary>
public class MappedKeyVaultSecretManager : KeyVaultSecretManager
{
    private readonly Dictionary<string, string> _secretMappings;

    public MappedKeyVaultSecretManager(Dictionary<string, string> secretMappings)
    {
        _secretMappings = secretMappings ?? throw new ArgumentNullException(nameof(secretMappings));
    }

    public override bool Load(SecretProperties properties)
    {
        // Only load secrets that are in our mapping
        return _secretMappings.ContainsKey(properties.Name);
    }

    public override string GetKey(KeyVaultSecret secret)
    {
        // Map the Key Vault secret name to the configuration key
        if (_secretMappings.TryGetValue(secret.Name, out var configKey))
        {
            return configKey;
        }

        // Fallback to default behavior (replace -- with :)
        return secret.Name.Replace("--", ConfigurationPath.KeyDelimiter);
    }
}

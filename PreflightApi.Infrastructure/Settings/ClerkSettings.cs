namespace PreflightApi.Infrastructure.Settings;

public class ClerkSettings
{
    /// <summary>Clerk JWKS authority URL (e.g., "https://your-instance.clerk.accounts.dev").</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>When false, API key management endpoints skip JWT auth in Development.</summary>
    public bool RequireAuthenticationInDevelopment { get; set; } = false;
}

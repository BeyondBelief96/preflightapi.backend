namespace PreflightApi.Infrastructure.Settings;

public class Auth0Settings
{
    public string? Auth0ApiIdentifier { get; set; }
    
    public string? Auth0Domain { get; set; }
    
    public bool RequireAuthenticationInDevelopment { get; set; }
}
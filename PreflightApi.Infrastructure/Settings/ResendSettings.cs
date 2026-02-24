namespace PreflightApi.Infrastructure.Settings
{
    public class ResendSettings
    {
        public string ApiToken { get; init; } = string.Empty;
        public string FromAddress { get; init; } = "alerts@contact.preflightapi.io";
        public string? ReplyToAddress { get; init; } = "bberisford@preflightapi.io";
        public bool Enabled { get; init; } = false;
        public int QuietPeriodMinutes { get; init; } = 60;
    }
}

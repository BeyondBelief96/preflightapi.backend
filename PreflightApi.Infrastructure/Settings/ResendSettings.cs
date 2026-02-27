namespace PreflightApi.Infrastructure.Settings
{
    public class ResendSettings
    {
        public string ApiToken { get; init; } = string.Empty;
        public string FromAddress { get; init; } = "alerts@contact.preflightapi.io";
        public string? ReplyToAddress { get; init; } = "bberisford@preflightapi.io";
        public bool Enabled { get; init; } = false;
        public int QuietPeriodMinutes { get; init; } = 1440;
        public string SegmentAllId { get; init; } = string.Empty;
        public string SegmentPaidId { get; init; } = string.Empty;
        public string SegmentFreeId { get; init; } = string.Empty;
        public string TopicAlertsId { get; init; } = string.Empty;
        public string ApiBaseUrl { get; init; } = "https://api.resend.com";
        public string HealthEndpointUrl { get; init; } = string.Empty;
    }
}

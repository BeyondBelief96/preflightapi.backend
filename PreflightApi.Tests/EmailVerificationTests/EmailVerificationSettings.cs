namespace PreflightApi.Tests.EmailVerificationTests
{
    public class EmailVerificationSettings
    {
        public bool SendLiveEmails { get; init; }
        public string TestEmailAddress { get; init; } = string.Empty;
        public string ResendApiToken { get; init; } = string.Empty;
        public string FromAddress { get; init; } = "PreflightAPI Test <alerts@contact.preflightapi.io>";
    }
}

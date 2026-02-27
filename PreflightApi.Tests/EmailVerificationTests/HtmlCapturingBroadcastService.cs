using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Tests.EmailVerificationTests
{
    public class HtmlCapturingBroadcastService : IBroadcastService
    {
        public string? CapturedName { get; private set; }
        public string? CapturedSubject { get; private set; }
        public string? CapturedHtml { get; private set; }

        public Task<string> SendBroadcastAsync(string name, string subject, string html,
            string segmentId, string? topicId = null, CancellationToken ct = default)
        {
            CapturedName = name;
            CapturedSubject = subject;
            CapturedHtml = html;
            return Task.FromResult("fake-broadcast-id");
        }

        public void Reset()
        {
            CapturedName = null;
            CapturedSubject = null;
            CapturedHtml = null;
        }
    }
}

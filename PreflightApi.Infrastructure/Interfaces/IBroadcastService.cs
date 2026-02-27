namespace PreflightApi.Infrastructure.Interfaces
{
    public interface IBroadcastService
    {
        Task<string> SendBroadcastAsync(string name, string subject, string html,
            string segmentId, string? topicId = null, CancellationToken ct = default);
    }
}

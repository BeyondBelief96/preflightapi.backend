using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Settings;

namespace PreflightApi.Infrastructure.Services
{
    public class ResendBroadcastService : IBroadcastService
    {
        private readonly HttpClient _httpClient;
        private readonly ResendSettings _settings;
        private readonly ILogger<ResendBroadcastService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public ResendBroadcastService(
            HttpClient httpClient,
            IOptions<ResendSettings> settings,
            ILogger<ResendBroadcastService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<string> SendBroadcastAsync(string name, string subject, string html,
            string segmentId, string? topicId = null, CancellationToken ct = default)
        {
            var payload = new BroadcastPayload
            {
                Name = name,
                SegmentId = segmentId,
                TopicId = topicId,
                From = _settings.FromAddress,
                ReplyTo = _settings.ReplyToAddress,
                Subject = subject,
                Html = html,
                Send = true
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_settings.ApiBaseUrl}/broadcasts", payload, JsonOptions, ct);

            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Resend broadcast failed with {StatusCode}: {Body}",
                    response.StatusCode, responseBody);
                throw new HttpRequestException(
                    $"Resend broadcast API returned {(int)response.StatusCode}: {responseBody}");
            }

            var result = JsonSerializer.Deserialize<BroadcastResponse>(responseBody, JsonOptions);
            var broadcastId = result?.Id ?? "unknown";

            _logger.LogInformation("Broadcast '{Name}' sent successfully (ID: {BroadcastId})", name, broadcastId);
            return broadcastId;
        }

        private sealed class BroadcastPayload
        {
            public string Name { get; init; } = default!;
            public string SegmentId { get; init; } = default!;
            public string? TopicId { get; init; }
            public string From { get; init; } = default!;
            public string? ReplyTo { get; init; }
            public string Subject { get; init; } = default!;
            public string Html { get; init; } = default!;
            public bool Send { get; init; }
        }

        private sealed class BroadcastResponse
        {
            public string? Id { get; init; }
        }
    }
}

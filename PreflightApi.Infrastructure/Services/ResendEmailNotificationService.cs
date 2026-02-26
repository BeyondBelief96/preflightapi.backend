using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Settings;

namespace PreflightApi.Infrastructure.Services
{
    public class ResendEmailNotificationService : IEmailNotificationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ResendSettings _settings;
        private readonly ILogger<ResendEmailNotificationService> _logger;

        public ResendEmailNotificationService(
            IHttpClientFactory httpClientFactory,
            IOptions<ResendSettings> settings,
            ILogger<ResendEmailNotificationService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendStalenessAlertAsync(IReadOnlyList<DataFreshnessResult> staleTypes, CancellationToken ct = default)
        {
            if (!_settings.Enabled)
            {
                _logger.LogDebug("Email notifications disabled — skipping staleness alert for {Count} type(s)", staleTypes.Count);
                return;
            }

            var subject = $"[PreflightApi] Data staleness alert — {staleTypes.Count} type(s) stale";
            var html = BuildStalenessHtml(staleTypes);

            await SendBroadcastAsync(subject, html, ct);
        }

        public async Task SendRecoveryNoticeAsync(IReadOnlyList<string> recoveredTypes, CancellationToken ct = default)
        {
            if (!_settings.Enabled)
            {
                _logger.LogDebug("Email notifications disabled — skipping recovery notice");
                return;
            }

            var typeList = string.Join(", ", recoveredTypes);
            var subject = $"[PreflightApi] Data recovered — {typeList}";
            var html = BuildRecoveryHtml(recoveredTypes);

            await SendBroadcastAsync(subject, html, ct);
        }

        private async Task SendBroadcastAsync(string subject, string html, CancellationToken ct)
        {
            var client = _httpClientFactory.CreateClient("Resend");

            var payload = new
            {
                name = subject,
                segment_id = _settings.DataAlertsSegmentId,
                topic_id = _settings.DataAlertsTopicId,
                from = _settings.FromAddress,
                reply_to = _settings.ReplyToAddress,
                subject,
                html,
                send = true
            };

            var response = await client.PostAsJsonAsync("/broadcasts", payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException(
                    $"Resend broadcast API returned {(int)response.StatusCode}: {body}");
            }

            _logger.LogInformation("Sent broadcast '{Subject}' via Resend", subject);
        }

        private static string BuildStalenessHtml(IReadOnlyList<DataFreshnessResult> staleTypes)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><body style='font-family:sans-serif;'>");
            sb.AppendLine("<h2>Data Staleness Alert</h2>");
            sb.AppendLine("<p>The following data types are stale:</p>");
            sb.AppendLine("<table style='border-collapse:collapse; width:100%;'>");
            sb.AppendLine("<tr style='background:#f2f2f2;'>");
            sb.AppendLine("<th style='padding:8px; border:1px solid #ddd; text-align:left;'>Sync Type</th>");
            sb.AppendLine("<th style='padding:8px; border:1px solid #ddd; text-align:left;'>Severity</th>");
            sb.AppendLine("<th style='padding:8px; border:1px solid #ddd; text-align:left;'>Last Synced</th>");
            sb.AppendLine("<th style='padding:8px; border:1px solid #ddd; text-align:left;'>Message</th>");
            sb.AppendLine("</tr>");

            foreach (var item in staleTypes)
            {
                var severityColor = item.Severity switch
                {
                    "critical" => "#dc3545",
                    "warning" => "#fd7e14",
                    _ => "#6c757d"
                };

                sb.AppendLine("<tr>");
                sb.AppendLine($"<td style='padding:8px; border:1px solid #ddd;'>{Encode(item.SyncType)}</td>");
                sb.AppendLine($"<td style='padding:8px; border:1px solid #ddd; color:{severityColor}; font-weight:bold;'>{Encode(item.Severity)}</td>");
                sb.AppendLine($"<td style='padding:8px; border:1px solid #ddd;'>{(item.LastSuccessfulSync?.ToString("u") ?? "Never")}</td>");
                sb.AppendLine($"<td style='padding:8px; border:1px solid #ddd;'>{Encode(item.Message)}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");
            sb.AppendLine($"<p style='color:#999; font-size:12px;'>Alert generated at {DateTime.UtcNow:u}</p>");
            sb.AppendLine("<p style='color:#999; font-size:12px;'><a href=\"{{{RESEND_UNSUBSCRIBE_URL}}}\">Unsubscribe from data alerts</a></p>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private static string BuildRecoveryHtml(IReadOnlyList<string> recoveredTypes)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><body style='font-family:sans-serif;'>");
            sb.AppendLine("<h2>Data Recovery Notice</h2>");
            sb.AppendLine("<p>The following data types have recovered and are now fresh:</p>");
            sb.AppendLine("<ul>");
            foreach (var type in recoveredTypes)
            {
                sb.AppendLine($"<li style='color:#28a745; font-weight:bold;'>{Encode(type)}</li>");
            }
            sb.AppendLine("</ul>");
            sb.AppendLine($"<p style='color:#999; font-size:12px;'>Notice generated at {DateTime.UtcNow:u}</p>");
            sb.AppendLine("<p style='color:#999; font-size:12px;'><a href=\"{{{RESEND_UNSUBSCRIBE_URL}}}\">Unsubscribe from data alerts</a></p>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private static string Encode(string value) =>
            System.Net.WebUtility.HtmlEncode(value);
    }
}

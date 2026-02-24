using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Settings;
using Resend;

namespace PreflightApi.Infrastructure.Services
{
    public class ResendEmailNotificationService : IEmailNotificationService
    {
        private readonly IResend _resend;
        private readonly IClerkUserService _clerkUserService;
        private readonly ResendSettings _settings;
        private readonly ILogger<ResendEmailNotificationService> _logger;

        public ResendEmailNotificationService(
            IResend resend,
            IClerkUserService clerkUserService,
            IOptions<ResendSettings> settings,
            ILogger<ResendEmailNotificationService> logger)
        {
            _resend = resend;
            _clerkUserService = clerkUserService;
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

            var recipients = await _clerkUserService.GetAllUserEmailsAsync(ct);
            if (recipients.Count == 0)
            {
                _logger.LogWarning("No user emails found — skipping staleness alert");
                return;
            }

            var subject = $"[PreflightApi] Data staleness alert — {staleTypes.Count} type(s) stale";
            var html = BuildStalenessHtml(staleTypes);

            await SendToAllAsync(recipients, subject, html, ct);
        }

        public async Task SendRecoveryNoticeAsync(IReadOnlyList<string> recoveredTypes, CancellationToken ct = default)
        {
            if (!_settings.Enabled)
            {
                _logger.LogDebug("Email notifications disabled — skipping recovery notice");
                return;
            }

            var recipients = await _clerkUserService.GetAllUserEmailsAsync(ct);
            if (recipients.Count == 0)
            {
                _logger.LogWarning("No user emails found — skipping recovery notice");
                return;
            }

            var typeList = string.Join(", ", recoveredTypes);
            var subject = $"[PreflightApi] Data recovered — {typeList}";
            var html = BuildRecoveryHtml(recoveredTypes);

            await SendToAllAsync(recipients, subject, html, ct);
        }

        private async Task SendToAllAsync(IReadOnlyList<string> recipients, string subject, string html, CancellationToken ct)
        {
            foreach (var email in recipients)
            {
                try
                {
                    ct.ThrowIfCancellationRequested();

                    var message = new EmailMessage
                    {
                        From = _settings.FromAddress,
                        Subject = subject,
                        HtmlBody = html,
                        ReplyTo = _settings.ReplyToAddress
                    };
                    message.To.Add(email);

                    await _resend.EmailSendAsync(message, ct);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send alert email to {Email}", email);
                }
            }

            _logger.LogInformation("Sent '{Subject}' to {Count} recipient(s)", subject, recipients.Count);
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
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private static string Encode(string value) =>
            System.Net.WebUtility.HtmlEncode(value);
    }
}

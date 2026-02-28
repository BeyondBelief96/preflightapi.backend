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
        private readonly IBroadcastService _broadcastService;
        private readonly ICloudStorageService _cloudStorageService;
        private readonly CloudStorageSettings _cloudStorageSettings;
        private readonly ResendSettings _settings;
        private readonly ILogger<ResendEmailNotificationService> _logger;

        private const string ColorHeaderBg = "#141844";
        private const string ColorAccent = "#37A4DC";
        private const string ColorBodyText = "#374151";
        private const string ColorMuted = "#6B7280";
        private const string ColorPageBg = "#F3F4F6";
        private const string ColorCardBg = "#FFFFFF";
        private const string ColorBorder = "#E5E7EB";
        private const string ColorTableHeaderBg = "#F9FAFB";
        private const string ColorTableStripeBg = "#F9FAFB";
        private const string FontStack = "-apple-system, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";

        private const string LogoBlobPath = "preflightapi-logo-and-name-2.png";
        private static readonly TimeSpan LogoUrlExpiry = TimeSpan.FromDays(7);

        // Sync type → (affected API endpoints, human-readable impact)
        private static readonly Dictionary<string, (string[] Endpoints, string Impact)> SyncTypeInfo = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Metar"] = (["/metars", "/briefing/route"], "METAR observations may be outdated. Current conditions at airports may not reflect actual weather."),
            ["Taf"] = (["/tafs", "/briefing/route"], "Terminal aerodrome forecasts may be outdated. Forecast conditions may no longer be accurate."),
            ["Pirep"] = (["/pireps", "/briefing/route"], "Pilot reports may be missing or outdated. Real-time conditions reported by other pilots are unavailable."),
            ["Sigmet"] = (["/sigmets", "/briefing/route"], "SIGMET advisories may be outdated. Significant weather hazards (thunderstorms, turbulence, icing) may not be current."),
            ["GAirmet"] = (["/g-airmets", "/briefing/route"], "G-AIRMET data may be outdated. Graphical advisories for IFR, turbulence, icing, and mountain obscuration may not be current."),
            ["NotamDelta"] = (["/notams", "/briefing/route"], "NOTAMs may be outdated. Airport closures, airspace restrictions, and navigation aid outages may not reflect current status."),
            ["Airport"] = (["/airports", "/briefing/route"], "Airport data may be outdated. Runway information, operating hours, and facility details may not reflect the latest FAA publication cycle."),
            ["Frequency"] = (["/communication-frequencies"], "Communication frequencies may be outdated. ATC, ATIS, and ground control frequencies may have changed."),
            ["Airspace"] = (["/airspaces"], "Airspace boundaries may be outdated and may not reflect the latest FAA publication cycle."),
            ["SpecialUseAirspace"] = (["/airspaces/special-use"], "Special use airspace (MOAs, restricted areas, prohibited areas) may be outdated."),
            ["Obstacle"] = (["/obstacles", "/briefing/route"], "Obstacle data may be outdated. New towers, antennas, or other obstructions may not be listed."),
            ["ObstacleDailyChange"] = (["/obstacles"], "Daily obstacle change data is stale. Recently added or modified obstacles may not be listed."),
            ["ChartSupplement"] = (["/chart-supplements"], "Chart supplement PDFs may be from a previous publication cycle."),
            ["TerminalProcedure"] = (["/terminal-procedures"], "Terminal procedure charts (IAPs, DPs, STARs) may be from a previous publication cycle."),
            ["Navaid"] = (["/navaids"], "NAVAID data may be outdated. VOR, VORTAC, NDB, and DME locations, frequencies, and operational status may not reflect the latest FAA publication cycle."),
        };

        // Health check service → (affected API endpoints, human-readable impact)
        private static readonly Dictionary<string, (string[] Endpoints, string Impact)> HealthCheckInfo = new(StringComparer.OrdinalIgnoreCase)
        {
            ["api"] = (["All endpoints"], "The API is unreachable. All endpoints are unavailable until connectivity is restored."),
            ["database"] = (["All endpoints"], "All API requests will fail. No aviation data can be served until database connectivity is restored."),
            ["blob-storage"] = (["/chart-supplements", "/terminal-procedures"], "Chart supplement and terminal procedure PDFs cannot be retrieved. Pilots will be unable to view approach plates, departure procedures, or chart supplements."),
            ["noaa-weather"] = (["/metars", "/tafs", "/pireps", "/sigmets", "/g-airmets", "/briefing/route"], "Weather data cannot be refreshed. METARs, TAFs, PIREPs, SIGMETs, and G-AIRMETs will become increasingly stale until the NOAA API recovers."),
            ["noaa-magvar"] = (["/navlog", "/e6b"], "Magnetic variation calculations may fail or use cached values. Navigation log headings and E6B wind corrections may be inaccurate."),
            ["faa-nms"] = (["/notams", "/briefing/route"], "NOTAMs cannot be refreshed. Airport closures, TFRs, and airspace restrictions will become increasingly stale until the FAA NMS API recovers."),
        };

        public ResendEmailNotificationService(
            IBroadcastService broadcastService,
            ICloudStorageService cloudStorageService,
            IOptions<CloudStorageSettings> cloudStorageSettings,
            IOptions<ResendSettings> settings,
            ILogger<ResendEmailNotificationService> logger)
        {
            _broadcastService = broadcastService;
            _cloudStorageService = cloudStorageService;
            _cloudStorageSettings = cloudStorageSettings.Value;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendStalenessAlertAsync(IReadOnlyList<DataCurrencyResult> staleTypes, CancellationToken ct = default)
        {
            if (!_settings.Enabled)
            {
                _logger.LogDebug("Email notifications disabled — skipping staleness alert for {Count} type(s)", staleTypes.Count);
                return;
            }

            var logoUrl = await GetLogoUrlAsync();
            var subject = $"[PreflightApi] Data staleness alert — {staleTypes.Count} type(s) stale";
            var html = BuildStalenessHtml(staleTypes, logoUrl);
            var name = $"data-staleness-alert-{DateTime.UtcNow:yyyyMMdd-HHmmss}";

            await _broadcastService.SendBroadcastAsync(
                name, subject, html, _settings.SegmentAllId, _settings.TopicAlertsId, ct);

            _logger.LogInformation("Sent staleness alert broadcast for {Count} type(s)", staleTypes.Count);
        }

        public async Task SendRecoveryNoticeAsync(IReadOnlyList<string> recoveredTypes, CancellationToken ct = default)
        {
            if (!_settings.Enabled)
            {
                _logger.LogDebug("Email notifications disabled — skipping recovery notice");
                return;
            }

            var logoUrl = await GetLogoUrlAsync();
            var typeList = string.Join(", ", recoveredTypes);
            var subject = $"[PreflightApi] Data recovered — {typeList}";
            var html = BuildRecoveryHtml(recoveredTypes, logoUrl);
            var name = $"data-recovery-notice-{DateTime.UtcNow:yyyyMMdd-HHmmss}";

            await _broadcastService.SendBroadcastAsync(
                name, subject, html, _settings.SegmentAllId, _settings.TopicAlertsId, ct);

            _logger.LogInformation("Sent recovery notice broadcast for: {Types}", typeList);
        }

        public async Task SendServiceOutageAlertAsync(IReadOnlyList<HealthCheckEntry> degradedServices, CancellationToken ct = default)
        {
            if (!_settings.Enabled)
            {
                _logger.LogDebug("Email notifications disabled — skipping service outage alert");
                return;
            }

            var logoUrl = await GetLogoUrlAsync();
            var subject = $"[PreflightApi] Service outage alert — {degradedServices.Count} service(s) affected";
            var html = BuildServiceOutageHtml(degradedServices, logoUrl);
            var name = $"service-outage-alert-{DateTime.UtcNow:yyyyMMdd-HHmmss}";

            await _broadcastService.SendBroadcastAsync(
                name, subject, html, _settings.SegmentAllId, _settings.TopicAlertsId, ct);

            _logger.LogInformation("Sent service outage alert broadcast for {Count} service(s)", degradedServices.Count);
        }

        public async Task SendServiceRecoveryNoticeAsync(IReadOnlyList<string> recoveredServices, CancellationToken ct = default)
        {
            if (!_settings.Enabled)
            {
                _logger.LogDebug("Email notifications disabled — skipping service recovery notice");
                return;
            }

            var logoUrl = await GetLogoUrlAsync();
            var serviceList = string.Join(", ", recoveredServices);
            var subject = $"[PreflightApi] Service recovered — {serviceList}";
            var html = BuildServiceRecoveryHtml(recoveredServices, logoUrl);
            var name = $"service-recovery-notice-{DateTime.UtcNow:yyyyMMdd-HHmmss}";

            await _broadcastService.SendBroadcastAsync(
                name, subject, html, _settings.SegmentAllId, _settings.TopicAlertsId, ct);

            _logger.LogInformation("Sent service recovery notice broadcast for: {Services}", serviceList);
        }

        // ── Shared helpers ────────────────────────────────────────────────

        private async Task<string?> GetLogoUrlAsync()
        {
            try
            {
                return await _cloudStorageService.GeneratePresignedUrlAsync(
                    _cloudStorageSettings.PreflightApiResourcesContainerName,
                    LogoBlobPath,
                    LogoUrlExpiry);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate logo URL — emails will be sent without logo");
                return null;
            }
        }

        private string BuildEmailWrapper(string title, string? logoUrl, Action<StringBuilder> contentBuilder) =>
            BuildEmailWrapper(title, logoUrl, _settings.StatusPageUrl, contentBuilder);

        private static string BuildEmailWrapper(string title, string? logoUrl, string? statusPageUrl, Action<StringBuilder> contentBuilder)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\" xmlns=\"http://www.w3.org/1999/xhtml\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"utf-8\" />");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />");
            sb.AppendLine("<title></title>");
            sb.AppendLine("<!--[if mso]><style>table,td{font-family:Segoe UI,Arial,sans-serif !important;}</style><![endif]-->");
            sb.AppendLine("</head>");
            sb.AppendLine($"<body style=\"margin:0; padding:0; background-color:{ColorPageBg}; font-family:{FontStack}; font-size:16px; line-height:1.5; color:{ColorBodyText}; -webkit-text-size-adjust:100%; -ms-text-size-adjust:100%;\">");

            // Outer wrapper table for centering
            sb.AppendLine($"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"background-color:{ColorPageBg};\">");
            sb.AppendLine("<tr><td align=\"center\" style=\"padding:24px 16px;\">");

            // Main content table (600px max)
            sb.AppendLine("<table role=\"presentation\" width=\"600\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"max-width:600px; width:100%; border-collapse:collapse;\">");

            // Header with logo
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td style=\"background-color:{ColorHeaderBg}; padding:20px 32px; border-radius:8px 8px 0 0;\">");
            if (!string.IsNullOrEmpty(logoUrl))
                sb.AppendLine($"<img src=\"{Encode(logoUrl)}\" alt=\"PreflightAPI\" height=\"40\" style=\"display:block; max-height:40px; width:auto; border:0;\" />");
            else
                sb.AppendLine($"<span style=\"font-size:18px; font-weight:700; color:#FFFFFF;\">PreflightAPI</span>");
            sb.AppendLine("</td></tr>");

            // Accent line
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td style=\"height:3px; background-color:{ColorAccent}; font-size:1px; line-height:1px;\">&nbsp;</td>");
            sb.AppendLine("</tr>");

            // Title bar
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td style=\"background-color:{ColorCardBg}; padding:24px 32px 0 32px; border-left:1px solid {ColorBorder}; border-right:1px solid {ColorBorder};\">");
            sb.AppendLine($"<h1 style=\"margin:0 0 16px 0; font-size:20px; font-weight:700; color:{ColorBodyText}; line-height:1.3;\">{Encode(title)}</h1>");
            sb.AppendLine("</td></tr>");

            // Body content
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td style=\"background-color:{ColorCardBg}; padding:0 32px 32px 32px; border-left:1px solid {ColorBorder}; border-right:1px solid {ColorBorder};\">");

            contentBuilder(sb);

            sb.AppendLine("</td></tr>");

            // Footer
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td style=\"background-color:{ColorCardBg}; padding:0 32px 24px 32px; border-left:1px solid {ColorBorder}; border-right:1px solid {ColorBorder}; border-bottom:1px solid {ColorBorder}; border-radius:0 0 8px 8px;\">");
            sb.AppendLine($"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\">");
            sb.AppendLine($"<tr><td colspan=\"2\" style=\"border-top:1px solid {ColorBorder}; padding-top:16px;\"></td></tr>");
            if (!string.IsNullOrWhiteSpace(statusPageUrl))
            {
                sb.AppendLine("<tr><td colspan=\"2\" style=\"padding-bottom:8px;\">");
                sb.AppendLine($"<p style=\"margin:0; font-size:14px;\"><a href=\"{Encode(statusPageUrl)}\" style=\"color:{ColorAccent}; text-decoration:underline;\">View live service status</a></p>");
                sb.AppendLine("</td></tr>");
            }
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td style=\"color:{ColorMuted}; font-size:12px;\">Generated at {DateTime.UtcNow:u}</td>");
            sb.AppendLine($"<td style=\"color:{ColorMuted}; font-size:12px; text-align:right;\"><a href=\"{{{{{{RESEND_UNSUBSCRIBE_URL}}}}}}\" style=\"color:{ColorAccent}; text-decoration:underline;\">Manage your email preferences</a></td>");
            sb.AppendLine("</tr></table>");
            sb.AppendLine("</td></tr>");

            // Close main + outer tables
            sb.AppendLine("</table>");
            sb.AppendLine("</td></tr></table>");
            sb.AppendLine("</body></html>");

            return sb.ToString();
        }

        private static void AppendSeverityBadge(StringBuilder sb, string severity)
        {
            var (bgColor, textColor, label) = severity.ToLowerInvariant() switch
            {
                "critical" => ("#FEE2E2", "#991B1B", "Critical"),
                "warning" => ("#FEF3C7", "#92400E", "Warning"),
                _ => ("#F3F4F6", "#374151", severity)
            };
            sb.Append($"<span style=\"display:inline-block; padding:4px 12px; border-radius:12px; font-size:13px; font-weight:600; background-color:{bgColor}; color:{textColor};\">{Encode(label)}</span>");
        }

        private static void AppendStatusBadge(StringBuilder sb, string status)
        {
            var (bgColor, textColor, label) = status switch
            {
                "Unhealthy" => ("#FEE2E2", "#991B1B", "Unhealthy"),
                "Degraded" => ("#FEF3C7", "#92400E", "Degraded"),
                "Healthy" => ("#DCFCE7", "#166534", "Healthy"),
                _ => ("#F3F4F6", "#374151", status)
            };
            sb.Append($"<span style=\"display:inline-block; padding:4px 12px; border-radius:12px; font-size:13px; font-weight:600; background-color:{bgColor}; color:{textColor};\">{Encode(label)}</span>");
        }

        // ── Template builders ─────────────────────────────────────────────

        private string BuildStalenessHtml(IReadOnlyList<DataCurrencyResult> staleTypes, string logoUrl)
        {
            return BuildEmailWrapper("Data Staleness Alert", logoUrl, sb =>
            {
                sb.AppendLine($"<p style=\"margin:0 0 20px 0; color:{ColorBodyText};\">The following data types are stale and may be serving outdated information:</p>");

                // Per-type cards
                foreach (var item in staleTypes)
                {
                    var lastSynced = item.LastSuccessfulSync?.ToString("u") ?? "Never";
                    var info = SyncTypeInfo.GetValueOrDefault(item.SyncType);

                    AppendCardStart(sb);

                    // Header row: type name + severity badge
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td style=\"padding:0 0 8px 0; font-size:16px; font-weight:700; color:{ColorBodyText};\">{Encode(item.SyncType)}</td>");
                    sb.Append($"<td style=\"padding:0 0 8px 0; text-align:right;\">");
                    AppendSeverityBadge(sb, item.Severity);
                    sb.AppendLine("</td>");
                    sb.AppendLine("</tr>");

                    // Detail + last synced
                    sb.AppendLine("<tr><td colspan=\"2\">");
                    sb.AppendLine($"<p style=\"margin:0 0 4px 0; font-size:14px; color:{ColorBodyText};\">{Encode(item.Message)}</p>");
                    sb.AppendLine($"<p style=\"margin:0 0 8px 0; font-size:13px; color:{ColorMuted};\">Last synced: {lastSynced}</p>");

                    // Impact description
                    if (info.Impact != null)
                    {
                        sb.AppendLine($"<p style=\"margin:0 0 8px 0; font-size:14px; color:{ColorBodyText}; font-style:italic;\">{Encode(info.Impact)}</p>");
                    }

                    // Affected endpoints
                    if (info.Endpoints?.Length > 0)
                    {
                        sb.AppendLine($"<p style=\"margin:0 0 4px 0; font-size:13px; font-weight:600; color:{ColorMuted};\">AFFECTED ENDPOINTS</p>");
                        AppendEndpointPills(sb, info.Endpoints);
                    }

                    sb.AppendLine("</td></tr>");
                    AppendCardEnd(sb);
                }
            });
        }

        private string BuildRecoveryHtml(IReadOnlyList<string> recoveredTypes, string logoUrl)
        {
            return BuildEmailWrapper("Data Recovery Notice", logoUrl, sb =>
            {
                sb.AppendLine($"<p style=\"margin:0 0 20px 0; color:{ColorBodyText};\">The following data types have recovered and are now serving fresh data:</p>");

                foreach (var type in recoveredTypes)
                {
                    var info = SyncTypeInfo.GetValueOrDefault(type);

                    AppendCardStart(sb);

                    // Header row: type name + Healthy badge
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td style=\"padding:0 0 8px 0; font-size:16px; font-weight:700; color:{ColorBodyText};\">{Encode(type)}</td>");
                    sb.Append($"<td style=\"padding:0 0 8px 0; text-align:right;\">");
                    AppendStatusBadge(sb, "Healthy");
                    sb.AppendLine("</td>");
                    sb.AppendLine("</tr>");

                    sb.AppendLine("<tr><td colspan=\"2\">");

                    // Restored capabilities
                    if (info.Endpoints?.Length > 0)
                    {
                        sb.AppendLine($"<p style=\"margin:0 0 4px 0; font-size:13px; font-weight:600; color:{ColorMuted};\">RESTORED ENDPOINTS</p>");
                        AppendEndpointPills(sb, info.Endpoints);
                    }

                    sb.AppendLine("</td></tr>");
                    AppendCardEnd(sb);
                }
            });
        }

        private string BuildServiceOutageHtml(IReadOnlyList<HealthCheckEntry> degradedServices, string logoUrl)
        {
            return BuildEmailWrapper("Service Outage Alert", logoUrl, sb =>
            {
                sb.AppendLine($"<p style=\"margin:0 0 20px 0; color:{ColorBodyText};\">The following services are experiencing issues and may affect API availability:</p>");

                // Per-service cards
                foreach (var service in degradedServices)
                {
                    var info = HealthCheckInfo.GetValueOrDefault(service.Name);

                    AppendCardStart(sb);

                    // Header row: service name + status badge
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td style=\"padding:0 0 8px 0; font-size:16px; font-weight:700; color:{ColorBodyText};\">{Encode(service.Name)}</td>");
                    sb.Append($"<td style=\"padding:0 0 8px 0; text-align:right;\">");
                    AppendStatusBadge(sb, service.Status);
                    sb.AppendLine("</td>");
                    sb.AppendLine("</tr>");

                    // Description
                    sb.AppendLine("<tr><td colspan=\"2\">");
                    sb.AppendLine($"<p style=\"margin:0 0 8px 0; font-size:14px; color:{ColorBodyText};\">{Encode(service.Description ?? "No details available")}</p>");

                    // Impact description
                    if (info.Impact != null)
                    {
                        sb.AppendLine($"<p style=\"margin:0 0 8px 0; font-size:14px; color:{ColorBodyText}; font-style:italic;\">{Encode(info.Impact)}</p>");
                    }

                    // Affected endpoints
                    if (info.Endpoints?.Length > 0)
                    {
                        sb.AppendLine($"<p style=\"margin:0 0 4px 0; font-size:13px; font-weight:600; color:{ColorMuted};\">AFFECTED ENDPOINTS</p>");
                        AppendEndpointPills(sb, info.Endpoints);
                    }

                    sb.AppendLine("</td></tr>");
                    AppendCardEnd(sb);
                }
            });
        }

        private string BuildServiceRecoveryHtml(IReadOnlyList<string> recoveredServices, string logoUrl)
        {
            return BuildEmailWrapper("Service Recovery Notice", logoUrl, sb =>
            {
                sb.AppendLine($"<p style=\"margin:0 0 20px 0; color:{ColorBodyText};\">The following services have recovered and are now operating normally:</p>");

                foreach (var service in recoveredServices)
                {
                    var info = HealthCheckInfo.GetValueOrDefault(service);

                    AppendCardStart(sb);

                    // Header row: service name + Healthy badge
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td style=\"padding:0 0 8px 0; font-size:16px; font-weight:700; color:{ColorBodyText};\">{Encode(service)}</td>");
                    sb.Append($"<td style=\"padding:0 0 8px 0; text-align:right;\">");
                    AppendStatusBadge(sb, "Healthy");
                    sb.AppendLine("</td>");
                    sb.AppendLine("</tr>");

                    sb.AppendLine("<tr><td colspan=\"2\">");

                    // Restored capabilities
                    if (info.Endpoints?.Length > 0)
                    {
                        sb.AppendLine($"<p style=\"margin:0 0 4px 0; font-size:13px; font-weight:600; color:{ColorMuted};\">RESTORED ENDPOINTS</p>");
                        AppendEndpointPills(sb, info.Endpoints);
                    }

                    sb.AppendLine("</td></tr>");
                    AppendCardEnd(sb);
                }
            });
        }

        private static void AppendCardStart(StringBuilder sb)
        {
            sb.AppendLine($"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"margin-bottom:16px; border:1px solid {ColorBorder}; border-radius:6px;\">");
            sb.AppendLine($"<tr><td style=\"padding:16px;\">");
            sb.AppendLine("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\">");
        }

        private static void AppendCardEnd(StringBuilder sb)
        {
            sb.AppendLine("</table>");
            sb.AppendLine("</td></tr></table>");
        }

        private static void AppendEndpointPills(StringBuilder sb, IEnumerable<string> endpoints)
        {
            // Using individual inline-block spans for email-safe "pill" layout
            sb.Append("<p style=\"margin:0; line-height:2;\">");
            foreach (var endpoint in endpoints.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(e => e))
            {
                sb.Append($"<code style=\"display:inline-block; background-color:{ColorTableHeaderBg}; padding:2px 8px; border-radius:3px; font-size:13px; margin-right:6px; margin-bottom:4px; white-space:nowrap;\">{Encode(endpoint)}</code>");
            }
            sb.AppendLine("</p>");
        }

        private static string Encode(string value) =>
            System.Net.WebUtility.HtmlEncode(value);
    }
}

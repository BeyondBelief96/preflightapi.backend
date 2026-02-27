using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Services;
using PreflightApi.Infrastructure.Services.CloudStorage;
using PreflightApi.Infrastructure.Settings;
using Xunit;
using Xunit.Abstractions;

namespace PreflightApi.Tests.EmailVerificationTests
{
    public class EmailVerificationTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly HtmlCapturingBroadcastService _capturingService;
        private readonly ResendEmailNotificationService _notificationService;
        private readonly ResendDirectEmailSender? _emailSender;
        private readonly EmailVerificationSettings _settings;
        private readonly string _outputDir;

        public EmailVerificationTests(ITestOutputHelper output)
        {
            _output = output;

            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("EmailVerificationTests/testsettings.json", optional: true)
                .Build();

            _settings = new EmailVerificationSettings();
            config.GetSection("EmailVerification").Bind(_settings);

            _capturingService = new HtmlCapturingBroadcastService();

            var resendSettings = Options.Create(new ResendSettings
            {
                Enabled = true,
                SegmentAllId = "fake-segment-id",
                TopicAlertsId = "fake-topic-id"
            });

            var cloudStorageSettings = new CloudStorageSettings();
            config.GetSection("CloudStorage").Bind(cloudStorageSettings);

            ICloudStorageService cloudStorageService;
            if (!string.IsNullOrEmpty(cloudStorageSettings.ConnectionString))
            {
                cloudStorageService = new AzureBlobStorageService(
                    Options.Create(cloudStorageSettings),
                    NullLogger<AzureBlobStorageService>.Instance);
                _output.WriteLine("Cloud storage: using real Azure Blob Storage (connection string found)");
            }
            else
            {
                var mock = Substitute.For<ICloudStorageService>();
                mock.GeneratePresignedUrlAsync(
                    Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>())
                    .Returns("https://storage.example.com/logo.png?sig=placeholder");
                cloudStorageService = mock;
                _output.WriteLine("Cloud storage: using mock (no CloudStorage:ConnectionString in testsettings.json)");
            }

            _notificationService = new ResendEmailNotificationService(
                _capturingService,
                cloudStorageService,
                Options.Create(cloudStorageSettings),
                resendSettings,
                NullLogger<ResendEmailNotificationService>.Instance);

            if (_settings.SendLiveEmails &&
                !string.IsNullOrEmpty(_settings.ResendApiToken) &&
                !string.IsNullOrEmpty(_settings.TestEmailAddress))
            {
                _emailSender = new ResendDirectEmailSender(
                    _settings.ResendApiToken, _settings.FromAddress, _settings.TestEmailAddress);
                _output.WriteLine($"Live email mode: sending to {_settings.TestEmailAddress}");
            }
            else
            {
                _output.WriteLine("HTML-only mode (no live emails)");
            }

            _outputDir = Path.Combine(AppContext.BaseDirectory, "EmailVerificationOutput");
            Directory.CreateDirectory(_outputDir);
        }

        [Fact]
        public async Task Verify_StalenessAlert_Template()
        {
            var staleTypes = new List<DataCurrencyResult>
            {
                new()
                {
                    SyncType = "Metar", Severity = "critical", StalenessMode = "time",
                    LastSuccessfulSync = DateTime.UtcNow.AddHours(-2), AgeMinutes = 120,
                    ThresholdMinutes = 30, Message = "METAR data is 120 minutes old (threshold: 30 min)"
                },
                new()
                {
                    SyncType = "Taf", Severity = "warning", StalenessMode = "time",
                    LastSuccessfulSync = DateTime.UtcNow.AddHours(-1), AgeMinutes = 60,
                    ThresholdMinutes = 45, Message = "TAF data is 60 minutes old (threshold: 45 min)"
                },
                new()
                {
                    SyncType = "Pirep", Severity = "warning", StalenessMode = "time",
                    LastSuccessfulSync = DateTime.UtcNow.AddMinutes(-90), AgeMinutes = 90,
                    ThresholdMinutes = 60, Message = "PIREP data is 90 minutes old (threshold: 60 min)"
                },
                new()
                {
                    SyncType = "Airport", Severity = "critical", StalenessMode = "cycle",
                    LastSuccessfulSync = DateTime.UtcNow.AddDays(-30),
                    CurrentCycleDate = DateTime.UtcNow.AddDays(-2), DaysPastCycleWithoutUpdate = 2,
                    Message = "Airport data is 2 days past cycle without update"
                }
            };

            await _notificationService.SendStalenessAlertAsync(staleTypes);

            _capturingService.CapturedHtml.Should().NotBeNullOrEmpty();
            _capturingService.CapturedSubject.Should().Contain("staleness");

            await WriteAndOptionallySend("staleness-alert");
        }

        [Fact]
        public async Task Verify_RecoveryNotice_Template()
        {
            var recoveredTypes = new List<string> { "Metar", "Taf", "Pirep" };

            await _notificationService.SendRecoveryNoticeAsync(recoveredTypes);

            _capturingService.CapturedHtml.Should().NotBeNullOrEmpty();
            _capturingService.CapturedSubject.Should().Contain("recovered");

            await WriteAndOptionallySend("recovery-notice");
        }

        [Fact]
        public async Task Verify_ServiceOutageAlert_Template()
        {
            var degradedServices = new List<HealthCheckEntry>
            {
                new()
                {
                    Name = "noaa-weather", Status = "Unhealthy",
                    Description = "NOAA API returned 503 Service Unavailable"
                },
                new()
                {
                    Name = "database", Status = "Degraded",
                    Description = "Connection pool exhausted, queries slow"
                },
                new()
                {
                    Name = "faa-nms", Status = "Unhealthy",
                    Description = "FAA NMS auth endpoint unreachable"
                }
            };

            await _notificationService.SendServiceOutageAlertAsync(degradedServices);

            _capturingService.CapturedHtml.Should().NotBeNullOrEmpty();
            _capturingService.CapturedSubject.Should().Contain("outage");

            await WriteAndOptionallySend("service-outage-alert");
        }

        [Fact]
        public async Task Verify_ServiceRecoveryNotice_Template()
        {
            var recoveredServices = new List<string>
            {
                "noaa-weather", "database", "faa-nms"
            };

            await _notificationService.SendServiceRecoveryNoticeAsync(recoveredServices);

            _capturingService.CapturedHtml.Should().NotBeNullOrEmpty();
            _capturingService.CapturedSubject.Should().Contain("recovered");

            await WriteAndOptionallySend("service-recovery-notice");
        }

        private async Task WriteAndOptionallySend(string templateName)
        {
            var html = _capturingService.CapturedHtml!;
            var subject = _capturingService.CapturedSubject!;

            var filePath = Path.Combine(_outputDir, $"{templateName}.html");
            await File.WriteAllTextAsync(filePath, html);
            _output.WriteLine($"HTML written to: {filePath}");

            if (_emailSender != null)
            {
                // The transactional /emails API doesn't process broadcast placeholders,
                // so replace with a dummy URL so the link renders as clickable in Gmail.
                var liveHtml = html.Replace("{{{RESEND_UNSUBSCRIBE_URL}}}", "#unsubscribe-test");

                // Resend rate limit: 2 requests/second
                await Task.Delay(1000);
                await _emailSender.SendAsync($"[TEST] {subject}", liveHtml);
                _output.WriteLine($"Email sent: [TEST] {subject}");
            }
        }

        public void Dispose() => _emailSender?.Dispose();
    }
}

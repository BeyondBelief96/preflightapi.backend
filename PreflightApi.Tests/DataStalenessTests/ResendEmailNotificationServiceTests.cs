using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Services;
using PreflightApi.Infrastructure.Settings;
using Xunit;

namespace PreflightApi.Tests.DataStalenessTests;

public class ResendEmailNotificationServiceTests
{
    private readonly IBroadcastService _broadcastService;
    private readonly ICloudStorageService _cloudStorageService;
    private readonly ResendSettings _settings;
    private readonly ILogger<ResendEmailNotificationService> _logger;
    private readonly ResendEmailNotificationService _service;

    public ResendEmailNotificationServiceTests()
    {
        _broadcastService = Substitute.For<IBroadcastService>();
        _cloudStorageService = Substitute.For<ICloudStorageService>();
        _settings = new ResendSettings
        {
            Enabled = true,
            FromAddress = "alerts@test.io",
            ReplyToAddress = "reply@test.io",
            QuietPeriodMinutes = 60,
            SegmentAllId = "seg-all-123",
            TopicAlertsId = "topic-alerts-456"
        };
        _logger = Substitute.For<ILogger<ResendEmailNotificationService>>();

        _broadcastService.SendBroadcastAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("broadcast-id-123");

        _cloudStorageService.GeneratePresignedUrlAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>())
            .Returns("https://storage.test/branding/logo.png?sig=fake");

        var cloudStorageSettings = new CloudStorageSettings
        {
            PreflightApiResourcesContainerName = "preflightapi-resources"
        };

        _service = new ResendEmailNotificationService(
            _broadcastService,
            _cloudStorageService,
            Options.Create(cloudStorageSettings),
            Options.Create(_settings),
            _logger);
    }

    #region Helpers

    private static List<DataCurrencyResult> MakeStaleTypes(int count)
    {
        return Enumerable.Range(1, count).Select(i => new DataCurrencyResult
        {
            SyncType = $"Type{i}",
            Severity = "warning",
            IsFresh = false,
            StalenessMode = "TimeBased",
            Message = $"Type{i} is stale",
            LastSuccessfulSync = DateTime.UtcNow.AddHours(-i)
        }).ToList();
    }

    private ResendEmailNotificationService CreateDisabledService()
    {
        var disabledSettings = new ResendSettings
        {
            Enabled = false,
            FromAddress = "alerts@test.io"
        };
        return new ResendEmailNotificationService(
            _broadcastService,
            _cloudStorageService,
            Options.Create(new CloudStorageSettings()),
            Options.Create(disabledSettings),
            _logger);
    }

    #endregion

    #region SendStalenessAlertAsync

    [Fact]
    public async Task SendStalenessAlert_Disabled_DoesNotSendBroadcast()
    {
        // Arrange
        var service = CreateDisabledService();

        // Act
        await service.SendStalenessAlertAsync(MakeStaleTypes(2));

        // Assert
        await _broadcastService.DidNotReceive().SendBroadcastAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendStalenessAlert_SendsSingleBroadcast()
    {
        // Act
        await _service.SendStalenessAlertAsync(MakeStaleTypes(3));

        // Assert — single broadcast call, not per-recipient
        await _broadcastService.Received(1).SendBroadcastAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendStalenessAlert_UsesCorrectSegmentAndTopic()
    {
        // Act
        await _service.SendStalenessAlertAsync(MakeStaleTypes(1));

        // Assert
        await _broadcastService.Received(1).SendBroadcastAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            "seg-all-123",
            "topic-alerts-456",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendStalenessAlert_SubjectContainsCount()
    {
        // Act
        await _service.SendStalenessAlertAsync(MakeStaleTypes(3));

        // Assert
        await _broadcastService.Received(1).SendBroadcastAsync(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("3 type(s) stale")),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendStalenessAlert_HtmlContainsUnsubscribeLink()
    {
        // Act
        await _service.SendStalenessAlertAsync(MakeStaleTypes(1));

        // Assert
        await _broadcastService.Received(1).SendBroadcastAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is<string>(html => html.Contains("{{{RESEND_UNSUBSCRIBE_URL}}}")),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendStalenessAlert_HtmlContainsSyncTypeAndSeverity()
    {
        // Arrange
        var staleTypes = new List<DataCurrencyResult>
        {
            new()
            {
                SyncType = "Metar",
                Severity = "critical",
                IsFresh = false,
                StalenessMode = "TimeBased",
                Message = "Metar is critically stale",
                LastSuccessfulSync = DateTime.UtcNow.AddHours(-2)
            }
        };

        // Act
        await _service.SendStalenessAlertAsync(staleTypes);

        // Assert
        await _broadcastService.Received(1).SendBroadcastAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is<string>(html => html.Contains("Metar") && html.Contains("critical")),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region SendRecoveryNoticeAsync

    [Fact]
    public async Task SendRecoveryNotice_Disabled_DoesNotSend()
    {
        // Arrange
        var service = CreateDisabledService();

        // Act
        await service.SendRecoveryNoticeAsync(new List<string> { "Metar", "Taf" });

        // Assert
        await _broadcastService.DidNotReceive().SendBroadcastAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendRecoveryNotice_SendsSingleBroadcast()
    {
        // Act
        await _service.SendRecoveryNoticeAsync(new List<string> { "Metar", "Taf" });

        // Assert
        await _broadcastService.Received(1).SendBroadcastAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendRecoveryNotice_SubjectContainsTypeNames()
    {
        // Act
        await _service.SendRecoveryNoticeAsync(new List<string> { "Metar", "Taf" });

        // Assert
        await _broadcastService.Received(1).SendBroadcastAsync(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("Metar") && s.Contains("Taf")),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendRecoveryNotice_UsesCorrectSegmentAndTopic()
    {
        // Act
        await _service.SendRecoveryNoticeAsync(new List<string> { "Metar" });

        // Assert
        await _broadcastService.Received(1).SendBroadcastAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            "seg-all-123",
            "topic-alerts-456",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendRecoveryNotice_HtmlContainsUnsubscribeLink()
    {
        // Act
        await _service.SendRecoveryNoticeAsync(new List<string> { "Metar" });

        // Assert
        await _broadcastService.Received(1).SendBroadcastAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is<string>(html => html.Contains("{{{RESEND_UNSUBSCRIBE_URL}}}")),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region SendServiceOutageAlertAsync

    [Fact]
    public async Task SendServiceOutageAlert_Disabled_DoesNotSend()
    {
        // Arrange
        var service = CreateDisabledService();
        var checks = new List<HealthCheckEntry>
        {
            new() { Name = "database", Status = "Unhealthy", Description = "Connection failed" }
        };

        // Act
        await service.SendServiceOutageAlertAsync(checks);

        // Assert
        await _broadcastService.DidNotReceive().SendBroadcastAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendServiceOutageAlert_SendsSingleBroadcast()
    {
        // Arrange
        var checks = new List<HealthCheckEntry>
        {
            new() { Name = "database", Status = "Unhealthy", Description = "Connection failed" },
            new() { Name = "blob-storage", Status = "Degraded", Description = "Slow responses" }
        };

        // Act
        await _service.SendServiceOutageAlertAsync(checks);

        // Assert
        await _broadcastService.Received(1).SendBroadcastAsync(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("2 service(s) affected")),
            Arg.Is<string>(html => html.Contains("database") && html.Contains("blob-storage")),
            "seg-all-123",
            "topic-alerts-456",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendServiceOutageAlert_HtmlContainsStatusColors()
    {
        // Arrange
        var checks = new List<HealthCheckEntry>
        {
            new() { Name = "database", Status = "Unhealthy", Description = "Down" }
        };

        // Act
        await _service.SendServiceOutageAlertAsync(checks);

        // Assert
        await _broadcastService.Received(1).SendBroadcastAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is<string>(html => html.Contains("#991B1B") && html.Contains("Unhealthy")),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region SendServiceRecoveryNoticeAsync

    [Fact]
    public async Task SendServiceRecoveryNotice_Disabled_DoesNotSend()
    {
        // Arrange
        var service = CreateDisabledService();

        // Act
        await service.SendServiceRecoveryNoticeAsync(new List<string> { "database" });

        // Assert
        await _broadcastService.DidNotReceive().SendBroadcastAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendServiceRecoveryNotice_SendsSingleBroadcast()
    {
        // Act
        await _service.SendServiceRecoveryNoticeAsync(new List<string> { "database", "blob-storage" });

        // Assert
        await _broadcastService.Received(1).SendBroadcastAsync(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("database") && s.Contains("blob-storage")),
            Arg.Any<string>(),
            "seg-all-123",
            "topic-alerts-456",
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Cancellation

    [Fact]
    public async Task SendStalenessAlert_CancellationRequested_Throws()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        _broadcastService.SendBroadcastAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var act = () => _service.SendStalenessAlertAsync(MakeStaleTypes(1), cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion
}

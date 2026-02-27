using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Services;
using PreflightApi.Infrastructure.Settings;
using Resend;
using Xunit;

namespace PreflightApi.Tests.DataStalenessTests;

public class ResendEmailNotificationServiceTests
{
    private readonly IResend _resend;
    private readonly IClerkUserService _clerkUserService;
    private readonly ResendSettings _settings;
    private readonly ILogger<ResendEmailNotificationService> _logger;
    private readonly ResendEmailNotificationService _service;

    public ResendEmailNotificationServiceTests()
    {
        _resend = Substitute.For<IResend>();
        _clerkUserService = Substitute.For<IClerkUserService>();
        _settings = new ResendSettings
        {
            Enabled = true,
            FromAddress = "alerts@test.io",
            ReplyToAddress = "reply@test.io",
            QuietPeriodMinutes = 60
        };
        _logger = Substitute.For<ILogger<ResendEmailNotificationService>>();

        _service = new ResendEmailNotificationService(
            _resend,
            _clerkUserService,
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

    private void SetupRecipients(params string[] emails)
    {
        _clerkUserService.GetAllUserEmailsAsync(Arg.Any<CancellationToken>())
            .Returns(emails.ToList().AsReadOnly());
    }

    private ResendEmailNotificationService CreateDisabledService()
    {
        var disabledSettings = new ResendSettings
        {
            Enabled = false,
            FromAddress = "alerts@test.io"
        };
        return new ResendEmailNotificationService(
            _resend, _clerkUserService, Options.Create(disabledSettings), _logger);
    }

    #endregion

    #region SendStalenessAlertAsync

    [Fact]
    public async Task SendStalenessAlert_Disabled_DoesNotSendOrFetchRecipients()
    {
        // Arrange
        var service = CreateDisabledService();

        // Act
        await service.SendStalenessAlertAsync(MakeStaleTypes(2));

        // Assert
        await _clerkUserService.DidNotReceive().GetAllUserEmailsAsync(Arg.Any<CancellationToken>());
        await _resend.DidNotReceive().EmailSendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendStalenessAlert_NoRecipients_LogsWarningAndReturns()
    {
        // Arrange
        SetupRecipients();

        // Act
        await _service.SendStalenessAlertAsync(MakeStaleTypes(2));

        // Assert
        await _resend.DidNotReceive().EmailSendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("No user emails found")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task SendStalenessAlert_SendsToAllRecipients()
    {
        // Arrange
        SetupRecipients("a@test.io", "b@test.io", "c@test.io");

        // Act
        await _service.SendStalenessAlertAsync(MakeStaleTypes(2));

        // Assert
        await _resend.Received(3).EmailSendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendStalenessAlert_SubjectContainsCount()
    {
        // Arrange
        SetupRecipients("a@test.io");

        // Act
        await _service.SendStalenessAlertAsync(MakeStaleTypes(3));

        // Assert
        await _resend.Received(1).EmailSendAsync(
            Arg.Is<EmailMessage>(m => m.Subject.Contains("3 type(s) stale")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendStalenessAlert_SetsFromAndReplyTo()
    {
        // Arrange
        SetupRecipients("a@test.io");

        // Act
        await _service.SendStalenessAlertAsync(MakeStaleTypes(1));

        // Assert
        var call = _resend.ReceivedCalls().Single();
        var message = (EmailMessage)call.GetArguments()[0]!;
        message.From.ToString().Should().Contain("alerts@test.io");
        string.Join(",", message.ReplyTo).Should().Contain("reply@test.io");
    }

    [Fact]
    public async Task SendStalenessAlert_OneRecipientFails_OthersStillReceive()
    {
        // Arrange
        SetupRecipients("a@test.io", "b@test.io", "c@test.io");
        var callCount = 0;
        _resend.When(x => x.EmailSendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>()))
            .Do(_ =>
            {
                if (Interlocked.Increment(ref callCount) == 2)
                    throw new HttpRequestException("API error");
            });

        // Act
        await _service.SendStalenessAlertAsync(MakeStaleTypes(1));

        // Assert — all 3 calls were attempted
        await _resend.Received(3).EmailSendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
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
        await _resend.DidNotReceive().EmailSendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendRecoveryNotice_NoRecipients_LogsWarningAndReturns()
    {
        // Arrange
        SetupRecipients();

        // Act
        await _service.SendRecoveryNoticeAsync(new List<string> { "Metar" });

        // Assert
        await _resend.DidNotReceive().EmailSendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendRecoveryNotice_SendsToAllRecipients()
    {
        // Arrange
        SetupRecipients("a@test.io", "b@test.io");

        // Act
        await _service.SendRecoveryNoticeAsync(new List<string> { "Metar" });

        // Assert
        await _resend.Received(2).EmailSendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendRecoveryNotice_SubjectContainsTypeNames()
    {
        // Arrange
        SetupRecipients("a@test.io");

        // Act
        await _service.SendRecoveryNoticeAsync(new List<string> { "Metar", "Taf" });

        // Assert
        await _resend.Received(1).EmailSendAsync(
            Arg.Is<EmailMessage>(m =>
                m.Subject.Contains("Metar") && m.Subject.Contains("Taf")),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Cancellation

    [Fact]
    public async Task SendStalenessAlert_CancellationRequested_Throws()
    {
        // Arrange
        SetupRecipients("a@test.io", "b@test.io");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => _service.SendStalenessAlertAsync(MakeStaleTypes(1), cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion
}

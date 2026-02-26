using System.Reflection;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using PreflightApi.Azure.Functions.Functions;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Settings;
using Xunit;

namespace PreflightApi.Tests.DataStalenessTests;

public class DataCurrencyAlertFunctionTests
{
    private readonly IDataSyncStatusService _syncService;
    private readonly IEmailNotificationService _emailService;
    private readonly ResendSettings _settings;
    private readonly DataCurrencyAlertFunction _function;
    private readonly FunctionContext _context;

    public DataCurrencyAlertFunctionTests()
    {
        _syncService = Substitute.For<IDataSyncStatusService>();
        _emailService = Substitute.For<IEmailNotificationService>();
        _settings = new ResendSettings { Enabled = true, QuietPeriodMinutes = 60 };

        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        _function = new DataCurrencyAlertFunction(
            _syncService,
            _emailService,
            Options.Create(_settings),
            loggerFactory);

        _context = Substitute.For<FunctionContext>();
    }

    #region Helpers

    private static DataCurrencyResult MakeResult(
        string syncType,
        string severity = "none",
        bool isFresh = true,
        DateTime? lastSuccessfulSync = null,
        DateTime? lastAlertSentUtc = null,
        string? lastAlertSeverity = null)
    {
        return new DataCurrencyResult
        {
            SyncType = syncType,
            Severity = severity,
            IsFresh = isFresh,
            StalenessMode = "TimeBased",
            LastSuccessfulSync = lastSuccessfulSync ?? (isFresh ? DateTime.UtcNow : null),
            Message = $"{syncType} test message",
            LastAlertSentUtc = lastAlertSentUtc,
            LastAlertSeverity = lastAlertSeverity
        };
    }

    private void SetupCurrency(params DataCurrencyResult[] results)
    {
        _syncService.GetAllCurrencyAsync(Arg.Any<CancellationToken>())
            .Returns(results.ToList().AsReadOnly());
    }

    private static bool InvokeSeverityEscalated(string? previous, string current)
    {
        var method = typeof(DataCurrencyAlertFunction)
            .GetMethod("SeverityEscalated", BindingFlags.NonPublic | BindingFlags.Static);
        return (bool)method!.Invoke(null, new object?[] { previous, current })!;
    }

    #endregion

    #region Alert Eligibility

    [Fact]
    public async Task Run_AllFresh_NoAlertsSent()
    {
        // Arrange
        SetupCurrency(
            MakeResult("Metar", "none", isFresh: true, lastSuccessfulSync: DateTime.UtcNow),
            MakeResult("Taf", "none", isFresh: true, lastSuccessfulSync: DateTime.UtcNow));

        // Act
        await _function.Run(null!, _context);

        // Assert
        await _emailService.DidNotReceive().SendStalenessAlertAsync(
            Arg.Any<IReadOnlyList<DataCurrencyResult>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_StaleWithNoAlertHistory_SendsAlert()
    {
        // Arrange — severity=warning, no prior alert
        SetupCurrency(
            MakeResult("Metar", "warning", isFresh: false,
                lastSuccessfulSync: DateTime.UtcNow.AddHours(-3),
                lastAlertSentUtc: null, lastAlertSeverity: null));

        // Act
        await _function.Run(null!, _context);

        // Assert
        await _emailService.Received(1).SendStalenessAlertAsync(
            Arg.Is<IReadOnlyList<DataCurrencyResult>>(l => l.Count == 1 && l[0].SyncType == "Metar"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_StaleWithinQuietPeriod_DoesNotSendAlert()
    {
        // Arrange — alerted 10 min ago, quiet period = 60 min, same severity
        SetupCurrency(
            MakeResult("Metar", "warning", isFresh: false,
                lastSuccessfulSync: DateTime.UtcNow.AddHours(-3),
                lastAlertSentUtc: DateTime.UtcNow.AddMinutes(-10),
                lastAlertSeverity: "warning"));

        // Act
        await _function.Run(null!, _context);

        // Assert
        await _emailService.DidNotReceive().SendStalenessAlertAsync(
            Arg.Any<IReadOnlyList<DataCurrencyResult>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_StaleQuietPeriodExpired_SendsAlert()
    {
        // Arrange — alerted 70 min ago, quiet period = 60 min
        SetupCurrency(
            MakeResult("Metar", "warning", isFresh: false,
                lastSuccessfulSync: DateTime.UtcNow.AddHours(-3),
                lastAlertSentUtc: DateTime.UtcNow.AddMinutes(-70),
                lastAlertSeverity: "warning"));

        // Act
        await _function.Run(null!, _context);

        // Assert
        await _emailService.Received(1).SendStalenessAlertAsync(
            Arg.Any<IReadOnlyList<DataCurrencyResult>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_SeverityEscalated_SendsAlertEvenDuringQuietPeriod()
    {
        // Arrange — was warning, now critical, alerted 10 min ago (within quiet period)
        SetupCurrency(
            MakeResult("Metar", "critical", isFresh: false,
                lastSuccessfulSync: DateTime.UtcNow.AddHours(-5),
                lastAlertSentUtc: DateTime.UtcNow.AddMinutes(-10),
                lastAlertSeverity: "warning"));

        // Act
        await _function.Run(null!, _context);

        // Assert
        await _emailService.Received(1).SendStalenessAlertAsync(
            Arg.Any<IReadOnlyList<DataCurrencyResult>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_SeverityDeescalated_DoesNotSendDuringQuietPeriod()
    {
        // Arrange — was critical, now warning, alerted 10 min ago (within quiet period)
        SetupCurrency(
            MakeResult("Metar", "warning", isFresh: false,
                lastSuccessfulSync: DateTime.UtcNow.AddHours(-3),
                lastAlertSentUtc: DateTime.UtcNow.AddMinutes(-10),
                lastAlertSeverity: "critical"));

        // Act
        await _function.Run(null!, _context);

        // Assert
        await _emailService.DidNotReceive().SendStalenessAlertAsync(
            Arg.Any<IReadOnlyList<DataCurrencyResult>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_NeverSyncedType_SkipsAlert()
    {
        // Arrange — severity=critical but LastSuccessfulSync=null (fresh deploy)
        SetupCurrency(
            MakeResult("Metar", "critical", isFresh: false,
                lastSuccessfulSync: null));

        // Act
        await _function.Run(null!, _context);

        // Assert
        await _emailService.DidNotReceive().SendStalenessAlertAsync(
            Arg.Any<IReadOnlyList<DataCurrencyResult>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_InfoSeverity_DoesNotTriggerAlert()
    {
        // Arrange — severity=info (rank < warning)
        SetupCurrency(
            MakeResult("Metar", "info", isFresh: false,
                lastSuccessfulSync: DateTime.UtcNow.AddHours(-1)));

        // Act
        await _function.Run(null!, _context);

        // Assert
        await _emailService.DidNotReceive().SendStalenessAlertAsync(
            Arg.Any<IReadOnlyList<DataCurrencyResult>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_NoneSeverity_DoesNotTriggerAlert()
    {
        // Arrange
        SetupCurrency(
            MakeResult("Metar", "none", isFresh: true, lastSuccessfulSync: DateTime.UtcNow));

        // Act
        await _function.Run(null!, _context);

        // Assert
        await _emailService.DidNotReceive().SendStalenessAlertAsync(
            Arg.Any<IReadOnlyList<DataCurrencyResult>>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Recovery Detection

    [Fact]
    public async Task Run_PreviouslyAlertedNowFresh_SendsRecoveryNotice()
    {
        // Arrange — was alerted (warning), now fresh
        SetupCurrency(
            MakeResult("Metar", "none", isFresh: true,
                lastSuccessfulSync: DateTime.UtcNow,
                lastAlertSentUtc: DateTime.UtcNow.AddHours(-1),
                lastAlertSeverity: "warning"));

        // Act
        await _function.Run(null!, _context);

        // Assert
        await _emailService.Received(1).SendRecoveryNoticeAsync(
            Arg.Is<IReadOnlyList<string>>(l => l.Contains("Metar")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_NeverAlertedAndFresh_NoRecovery()
    {
        // Arrange — fresh, no prior alert
        SetupCurrency(
            MakeResult("Metar", "none", isFresh: true,
                lastSuccessfulSync: DateTime.UtcNow,
                lastAlertSentUtc: null, lastAlertSeverity: null));

        // Act
        await _function.Run(null!, _context);

        // Assert
        await _emailService.DidNotReceive().SendRecoveryNoticeAsync(
            Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_StillStale_NoRecovery()
    {
        // Arrange — still stale, has prior alert
        SetupCurrency(
            MakeResult("Metar", "warning", isFresh: false,
                lastSuccessfulSync: DateTime.UtcNow.AddHours(-3),
                lastAlertSentUtc: DateTime.UtcNow.AddMinutes(-10),
                lastAlertSeverity: "warning"));

        // Act
        await _function.Run(null!, _context);

        // Assert
        await _emailService.DidNotReceive().SendRecoveryNoticeAsync(
            Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region State Management

    [Fact]
    public async Task Run_AlertSent_UpdatesAlertStateForEachType()
    {
        // Arrange — 2 types need alerts
        SetupCurrency(
            MakeResult("Metar", "warning", isFresh: false,
                lastSuccessfulSync: DateTime.UtcNow.AddHours(-3)),
            MakeResult("Taf", "critical", isFresh: false,
                lastSuccessfulSync: DateTime.UtcNow.AddHours(-5)));

        // Act
        await _function.Run(null!, _context);

        // Assert
        await _syncService.Received(1).UpdateAlertStateAsync("Metar", "warning", Arg.Any<CancellationToken>());
        await _syncService.Received(1).UpdateAlertStateAsync("Taf", "critical", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_RecoverySent_ClearsAlertStateForEachType()
    {
        // Arrange — 2 types recovered
        SetupCurrency(
            MakeResult("Metar", "none", isFresh: true,
                lastSuccessfulSync: DateTime.UtcNow,
                lastAlertSeverity: "warning"),
            MakeResult("Taf", "none", isFresh: true,
                lastSuccessfulSync: DateTime.UtcNow,
                lastAlertSeverity: "critical"));

        // Act
        await _function.Run(null!, _context);

        // Assert
        await _syncService.Received(1).ClearAlertStateAsync("Metar", Arg.Any<CancellationToken>());
        await _syncService.Received(1).ClearAlertStateAsync("Taf", Arg.Any<CancellationToken>());
    }

    #endregion

    #region Combined Scenarios

    [Fact]
    public async Task Run_MixedStaleAndRecovered_SendsBothAlertAndRecovery()
    {
        // Arrange
        SetupCurrency(
            // Stale type needing alert (no prior alert)
            MakeResult("Metar", "warning", isFresh: false,
                lastSuccessfulSync: DateTime.UtcNow.AddHours(-3)),
            // Recovered type
            MakeResult("Taf", "none", isFresh: true,
                lastSuccessfulSync: DateTime.UtcNow,
                lastAlertSeverity: "warning"));

        // Act
        await _function.Run(null!, _context);

        // Assert
        await _emailService.Received(1).SendStalenessAlertAsync(
            Arg.Any<IReadOnlyList<DataCurrencyResult>>(), Arg.Any<CancellationToken>());
        await _emailService.Received(1).SendRecoveryNoticeAsync(
            Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_NoActionNeeded_LogsNoAction()
    {
        // Arrange — all fresh, no prior alerts
        SetupCurrency(
            MakeResult("Metar", "none", isFresh: true, lastSuccessfulSync: DateTime.UtcNow),
            MakeResult("Taf", "none", isFresh: true, lastSuccessfulSync: DateTime.UtcNow));

        // Act
        await _function.Run(null!, _context);

        // Assert — no emails sent
        await _emailService.DidNotReceive().SendStalenessAlertAsync(
            Arg.Any<IReadOnlyList<DataCurrencyResult>>(), Arg.Any<CancellationToken>());
        await _emailService.DidNotReceive().SendRecoveryNoticeAsync(
            Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region SeverityEscalated Edge Cases

    [Fact]
    public void SeverityEscalated_NullPrevious_ReturnsTrue()
    {
        InvokeSeverityEscalated(null, "warning").Should().BeTrue();
    }

    [Fact]
    public void SeverityEscalated_SameLevel_ReturnsFalse()
    {
        InvokeSeverityEscalated("warning", "warning").Should().BeFalse();
    }

    [Fact]
    public void SeverityEscalated_HigherLevel_ReturnsTrue()
    {
        InvokeSeverityEscalated("warning", "critical").Should().BeTrue();
    }

    [Fact]
    public void SeverityEscalated_LowerLevel_ReturnsFalse()
    {
        InvokeSeverityEscalated("critical", "warning").Should().BeFalse();
    }

    [Fact]
    public void SeverityEscalated_UnknownSeverity_DefaultsToZero()
    {
        // Unknown strings get rank 0 from TryGetValue default
        // "warning" (rank 2) > "banana" (rank 0) → true
        InvokeSeverityEscalated("banana", "warning").Should().BeTrue();
        // "banana" (rank 0) > "none" (rank 0) → false (not strictly greater)
        InvokeSeverityEscalated("banana", "none").Should().BeFalse();
    }

    #endregion
}

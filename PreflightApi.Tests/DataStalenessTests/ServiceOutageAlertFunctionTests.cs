using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using PreflightApi.Azure.Functions.Functions;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Settings;
using RichardSzalay.MockHttp;
using Xunit;

namespace PreflightApi.Tests.DataStalenessTests;

public class ServiceOutageAlertFunctionTests
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly IServiceHealthAlertStateService _alertStateService;
    private readonly IEmailNotificationService _emailService;
    private readonly ResendSettings _settings;
    private readonly ServiceOutageAlertFunction _function;
    private readonly FunctionContext _context;

    public ServiceOutageAlertFunctionTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        _alertStateService = Substitute.For<IServiceHealthAlertStateService>();
        _emailService = Substitute.For<IEmailNotificationService>();
        _settings = new ResendSettings
        {
            Enabled = true,
            QuietPeriodMinutes = 60,
            HealthEndpointUrl = "https://api.example.com/health"
        };

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient("HealthEndpoint").Returns(_mockHttp.ToHttpClient());

        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        _function = new ServiceOutageAlertFunction(
            httpClientFactory,
            _alertStateService,
            _emailService,
            Options.Create(_settings),
            loggerFactory);

        _context = Substitute.For<FunctionContext>();

        // Default: no prior alert states
        _alertStateService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ServiceHealthAlertState>());
    }

    #region Helpers

    private void SetupHealthResponse(params HealthCheckEntry[] checks)
    {
        var response = new HealthCheckResponse
        {
            Status = checks.All(c => c.Status == "Healthy") ? "Healthy" : "Unhealthy",
            Version = "1.0.0",
            Checks = checks
        };
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _mockHttp.When(HttpMethod.Get, "https://api.example.com/health")
            .Respond("application/json", json);
    }

    private void SetupPriorStates(params ServiceHealthAlertState[] states)
    {
        _alertStateService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(states.ToList());
    }

    private static HealthCheckEntry MakeCheck(string name, string status, string? description = null) =>
        new() { Name = name, Status = status, Description = description };

    private static ServiceHealthAlertState MakePriorState(
        string serviceName,
        string lastKnownStatus = "Healthy",
        DateTime? lastAlertSentUtc = null,
        string? lastAlertSeverity = null) =>
        new()
        {
            ServiceName = serviceName,
            LastKnownStatus = lastKnownStatus,
            LastAlertSentUtc = lastAlertSentUtc,
            LastAlertSeverity = lastAlertSeverity,
            UpdatedAt = DateTime.UtcNow
        };

    #endregion

    #region All Healthy

    [Fact]
    public async Task Run_AllHealthy_NoAlertsSent()
    {
        // Arrange
        SetupHealthResponse(
            MakeCheck("database", "Healthy"),
            MakeCheck("blob-storage", "Healthy"));

        // Act
        await _function.Run(null!, _context);

        // Assert
        await _emailService.DidNotReceive().SendServiceOutageAlertAsync(
            Arg.Any<IReadOnlyList<HealthCheckEntry>>(), Arg.Any<CancellationToken>());
        await _emailService.DidNotReceive().SendServiceRecoveryNoticeAsync(
            Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Outage Detection

    [Fact]
    public async Task Run_DegradedService_NoPriorAlert_SendsAlert()
    {
        // Arrange
        SetupHealthResponse(
            MakeCheck("database", "Healthy"),
            MakeCheck("noaa-weather", "Degraded", "Slow responses"));

        // Act
        await _function.Run(null!, _context);

        // Assert
        await _emailService.Received(1).SendServiceOutageAlertAsync(
            Arg.Is<IReadOnlyList<HealthCheckEntry>>(list =>
                list.Count == 1 && list[0].Name == "noaa-weather"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_UnhealthyService_NoPriorAlert_SendsAlert()
    {
        // Arrange
        SetupHealthResponse(
            MakeCheck("database", "Unhealthy", "Connection refused"));

        // Act
        await _function.Run(null!, _context);

        // Assert
        await _emailService.Received(1).SendServiceOutageAlertAsync(
            Arg.Is<IReadOnlyList<HealthCheckEntry>>(list =>
                list.Count == 1 && list[0].Name == "database"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_MultipleUnhealthyServices_SendsSingleAlertWithAll()
    {
        // Arrange
        SetupHealthResponse(
            MakeCheck("database", "Unhealthy", "Down"),
            MakeCheck("blob-storage", "Degraded", "Slow"),
            MakeCheck("noaa-weather", "Healthy"));

        // Act
        await _function.Run(null!, _context);

        // Assert
        await _emailService.Received(1).SendServiceOutageAlertAsync(
            Arg.Is<IReadOnlyList<HealthCheckEntry>>(list => list.Count == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_DegradedService_UpdatesAlertState()
    {
        // Arrange
        SetupHealthResponse(MakeCheck("database", "Degraded", "Slow"));

        // Act
        await _function.Run(null!, _context);

        // Assert
        await _alertStateService.Received(1).UpdateAlertStateAsync("database", "degraded", Arg.Any<CancellationToken>());
    }

    #endregion

    #region Quiet Period

    [Fact]
    public async Task Run_WithinQuietPeriod_SameSeverity_DoesNotAlert()
    {
        // Arrange
        SetupHealthResponse(MakeCheck("database", "Degraded", "Slow"));
        SetupPriorStates(MakePriorState("database", "Degraded",
            lastAlertSentUtc: DateTime.UtcNow.AddMinutes(-30), // within 60 min quiet period
            lastAlertSeverity: "degraded"));

        // Act
        await _function.Run(null!, _context);

        // Assert
        await _emailService.DidNotReceive().SendServiceOutageAlertAsync(
            Arg.Any<IReadOnlyList<HealthCheckEntry>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_QuietPeriodExpired_SendsAlert()
    {
        // Arrange
        SetupHealthResponse(MakeCheck("database", "Degraded", "Still slow"));
        SetupPriorStates(MakePriorState("database", "Degraded",
            lastAlertSentUtc: DateTime.UtcNow.AddMinutes(-90), // past 60 min quiet period
            lastAlertSeverity: "degraded"));

        // Act
        await _function.Run(null!, _context);

        // Assert
        await _emailService.Received(1).SendServiceOutageAlertAsync(
            Arg.Is<IReadOnlyList<HealthCheckEntry>>(list => list.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_SeverityEscalated_AlertsEvenInQuietPeriod()
    {
        // Arrange — was Degraded, now Unhealthy, within quiet period
        SetupHealthResponse(MakeCheck("database", "Unhealthy", "Connection lost"));
        SetupPriorStates(MakePriorState("database", "Degraded",
            lastAlertSentUtc: DateTime.UtcNow.AddMinutes(-10),
            lastAlertSeverity: "degraded"));

        // Act
        await _function.Run(null!, _context);

        // Assert
        await _emailService.Received(1).SendServiceOutageAlertAsync(
            Arg.Is<IReadOnlyList<HealthCheckEntry>>(list =>
                list.Count == 1 && list[0].Name == "database"),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Recovery Detection

    [Fact]
    public async Task Run_PreviouslyAlerted_NowHealthy_SendsRecovery()
    {
        // Arrange
        SetupHealthResponse(MakeCheck("database", "Healthy"));
        SetupPriorStates(MakePriorState("database", "Unhealthy",
            lastAlertSentUtc: DateTime.UtcNow.AddMinutes(-30),
            lastAlertSeverity: "unhealthy"));

        // Act
        await _function.Run(null!, _context);

        // Assert
        await _emailService.Received(1).SendServiceRecoveryNoticeAsync(
            Arg.Is<IReadOnlyList<string>>(list =>
                list.Count == 1 && list[0] == "database"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_Recovery_ClearsAlertState()
    {
        // Arrange
        SetupHealthResponse(MakeCheck("database", "Healthy"));
        SetupPriorStates(MakePriorState("database", "Unhealthy",
            lastAlertSentUtc: DateTime.UtcNow.AddMinutes(-30),
            lastAlertSeverity: "unhealthy"));

        // Act
        await _function.Run(null!, _context);

        // Assert
        await _alertStateService.Received(1).ClearAlertStateAsync("database", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_NeverAlerted_NowHealthy_NoRecoverySent()
    {
        // Arrange
        SetupHealthResponse(MakeCheck("database", "Healthy"));
        SetupPriorStates(MakePriorState("database", "Healthy"));

        // Act
        await _function.Run(null!, _context);

        // Assert
        await _emailService.DidNotReceive().SendServiceRecoveryNoticeAsync(
            Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region API Unreachable

    [Fact]
    public async Task Run_ApiUnreachable_SendsAlertForSyntheticApiCheck()
    {
        // Arrange — health endpoint returns error
        _mockHttp.When(HttpMethod.Get, "https://api.example.com/health")
            .Respond(HttpStatusCode.ServiceUnavailable);

        // Act
        await _function.Run(null!, _context);

        // Assert
        await _emailService.Received(1).SendServiceOutageAlertAsync(
            Arg.Is<IReadOnlyList<HealthCheckEntry>>(list =>
                list.Count == 1 && list[0].Name == "api" && list[0].Status == "Unhealthy"),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Mixed Scenarios

    [Fact]
    public async Task Run_MixedDegradedAndRecovered_SendsBothAlerts()
    {
        // Arrange
        SetupHealthResponse(
            MakeCheck("database", "Unhealthy", "Down"),
            MakeCheck("blob-storage", "Healthy"));
        SetupPriorStates(
            MakePriorState("blob-storage", "Degraded",
                lastAlertSentUtc: DateTime.UtcNow.AddMinutes(-30),
                lastAlertSeverity: "degraded"));

        // Act
        await _function.Run(null!, _context);

        // Assert — outage alert for database + recovery for blob-storage
        await _emailService.Received(1).SendServiceOutageAlertAsync(
            Arg.Is<IReadOnlyList<HealthCheckEntry>>(list =>
                list.Count == 1 && list[0].Name == "database"),
            Arg.Any<CancellationToken>());
        await _emailService.Received(1).SendServiceRecoveryNoticeAsync(
            Arg.Is<IReadOnlyList<string>>(list =>
                list.Count == 1 && list[0] == "blob-storage"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_UpsertsStatusForAllChecks()
    {
        // Arrange
        SetupHealthResponse(
            MakeCheck("database", "Healthy"),
            MakeCheck("blob-storage", "Degraded", "Slow"));

        // Act
        await _function.Run(null!, _context);

        // Assert — both statuses upserted
        await _alertStateService.Received(1).UpsertStatusAsync("database", "Healthy", Arg.Any<CancellationToken>());
        await _alertStateService.Received(1).UpsertStatusAsync("blob-storage", "Degraded", Arg.Any<CancellationToken>());
    }

    #endregion

    #region FetchHealthChecksAsync

    [Fact]
    public async Task FetchHealthChecks_ValidResponse_ReturnsChecksWithSucceeded()
    {
        // Arrange
        SetupHealthResponse(
            MakeCheck("database", "Healthy"),
            MakeCheck("blob-storage", "Degraded", "Slow"));

        // Act
        var (checks, succeeded) = await _function.FetchHealthChecksAsync(CancellationToken.None);

        // Assert
        succeeded.Should().BeTrue();
        checks.Should().HaveCount(2);
        checks[0].Name.Should().Be("database");
        checks[1].Name.Should().Be("blob-storage");
    }

    [Fact]
    public async Task FetchHealthChecks_HttpError_ReturnsSyntheticApiCheckWithFailed()
    {
        // Arrange
        _mockHttp.When(HttpMethod.Get, "https://api.example.com/health")
            .Respond(HttpStatusCode.InternalServerError);

        // Act
        var (checks, succeeded) = await _function.FetchHealthChecksAsync(CancellationToken.None);

        // Assert
        succeeded.Should().BeFalse();
        checks.Should().HaveCount(1);
        checks[0].Name.Should().Be("api");
        checks[0].Status.Should().Be("Unhealthy");
        checks[0].Description.Should().Contain("Health endpoint unreachable");
    }

    #endregion

    #region Orphaned Recovery (synthetic "api" check)

    [Fact]
    public async Task Run_SyntheticApiAlertThenApiRecovers_SendsRecoveryForApi()
    {
        // Arrange — API is now reachable and healthy, but prior state has a synthetic "api" alert
        SetupHealthResponse(
            MakeCheck("database", "Healthy"),
            MakeCheck("blob-storage", "Healthy"));
        SetupPriorStates(MakePriorState("api", "Unhealthy",
            lastAlertSentUtc: DateTime.UtcNow.AddMinutes(-10),
            lastAlertSeverity: "unhealthy"));

        // Act
        await _function.Run(null!, _context);

        // Assert — recovery sent for orphaned "api" service
        await _emailService.Received(1).SendServiceRecoveryNoticeAsync(
            Arg.Is<IReadOnlyList<string>>(list =>
                list.Count == 1 && list[0] == "api"),
            Arg.Any<CancellationToken>());
        await _alertStateService.Received(1).ClearAlertStateAsync("api", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_SyntheticApiAlertAndApiStillDown_NoOrphanedRecovery()
    {
        // Arrange — API is still unreachable, so synthetic "api" check is still returned
        _mockHttp.When(HttpMethod.Get, "https://api.example.com/health")
            .Respond(HttpStatusCode.ServiceUnavailable);
        SetupPriorStates(MakePriorState("api", "Unhealthy",
            lastAlertSentUtc: DateTime.UtcNow.AddMinutes(-10),
            lastAlertSeverity: "unhealthy"));

        // Act
        await _function.Run(null!, _context);

        // Assert — no recovery since fetch failed (api still in the returned checks)
        await _emailService.DidNotReceive().SendServiceRecoveryNoticeAsync(
            Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }

    #endregion
}

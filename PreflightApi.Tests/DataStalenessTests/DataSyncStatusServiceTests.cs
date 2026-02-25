using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PreflightApi.Domain.Constants;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.ValueObjects.FaaPublications;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Services;
using Xunit;

namespace PreflightApi.Tests.DataStalenessTests;

public class DataSyncStatusServiceTests : IDisposable
{
    private readonly PreflightApiDbContext _dbContext;
    private readonly ILogger<DataSyncStatusService> _logger;
    private readonly DataSyncStatusService _service;

    public DataSyncStatusServiceTests()
    {
        var options = new DbContextOptionsBuilder<PreflightApiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PreflightApiDbContext(options);
        _logger = Substitute.For<ILogger<DataSyncStatusService>>();
        _service = new DataSyncStatusService(_dbContext, _logger);
    }

    public void Dispose() => _dbContext.Dispose();

    #region Helpers

    private void SeedStatus(params DataSyncStatus[] statuses)
    {
        _dbContext.DataSyncStatuses.AddRange(statuses);
        _dbContext.SaveChanges();
    }

    private void SeedCycles(params FaaPublicationCycle[] cycles)
    {
        _dbContext.FaaPublicationCycles.AddRange(cycles);
        _dbContext.SaveChanges();
    }

    private static DataSyncStatus MakeTimeBased(
        string syncType = "TestType",
        int threshold = 100,
        DateTime? lastSync = null,
        int consecutiveFailures = 0,
        string? errorMessage = null,
        DateTime? lastAlertSent = null,
        string? lastAlertSeverity = null)
    {
        return new DataSyncStatus
        {
            SyncType = syncType,
            StalenessMode = "TimeBased",
            StalenessThresholdMinutes = threshold,
            LastSuccessfulSyncUtc = lastSync,
            LastAttemptedSyncUtc = lastSync,
            LastSyncSucceeded = lastSync != null,
            ConsecutiveFailures = consecutiveFailures,
            LastErrorMessage = errorMessage,
            UpdatedAt = DateTime.UtcNow,
            LastAlertSentUtc = lastAlertSent,
            LastAlertSeverity = lastAlertSeverity
        };
    }

    private static DataSyncStatus MakeCycleBased(
        string syncType = "TestCycleType",
        string? publicationType = "ChartSupplement",
        DateTime? lastSync = null,
        int consecutiveFailures = 0,
        DateTime? lastAlertSent = null,
        string? lastAlertSeverity = null)
    {
        return new DataSyncStatus
        {
            SyncType = syncType,
            StalenessMode = "CycleBased",
            PublicationType = publicationType,
            LastSuccessfulSyncUtc = lastSync,
            LastAttemptedSyncUtc = lastSync,
            LastSyncSucceeded = lastSync != null,
            ConsecutiveFailures = consecutiveFailures,
            UpdatedAt = DateTime.UtcNow,
            LastAlertSentUtc = lastAlertSent,
            LastAlertSeverity = lastAlertSeverity
        };
    }

    #endregion

    #region RecordSuccessAsync

    [Fact]
    public async Task RecordSuccessAsync_ValidSyncType_UpdatesAllFields()
    {
        // Arrange
        SeedStatus(MakeTimeBased(SyncTypes.Metar, consecutiveFailures: 3, errorMessage: "old error"));

        // Act
        await _service.RecordSuccessAsync(SyncTypes.Metar, 42);

        // Assert
        var status = await _dbContext.DataSyncStatuses.FindAsync(SyncTypes.Metar);
        status!.LastSuccessfulSyncUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        status.LastAttemptedSyncUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        status.LastSyncSucceeded.Should().BeTrue();
        status.ConsecutiveFailures.Should().Be(0);
        status.LastErrorMessage.Should().BeNull();
        status.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RecordSuccessAsync_WithRecordCount_StoresCount()
    {
        // Arrange
        SeedStatus(MakeTimeBased(SyncTypes.Metar));

        // Act
        await _service.RecordSuccessAsync(SyncTypes.Metar, 42);

        // Assert
        var status = await _dbContext.DataSyncStatuses.FindAsync(SyncTypes.Metar);
        status!.LastSuccessfulRecordCount.Should().Be(42);
    }

    [Fact]
    public async Task RecordSuccessAsync_ResetsConsecutiveFailures_WhenPreviouslyFailed()
    {
        // Arrange
        SeedStatus(MakeTimeBased(SyncTypes.Metar, consecutiveFailures: 5));

        // Act
        await _service.RecordSuccessAsync(SyncTypes.Metar);

        // Assert
        var status = await _dbContext.DataSyncStatuses.FindAsync(SyncTypes.Metar);
        status!.ConsecutiveFailures.Should().Be(0);
    }

    [Fact]
    public async Task RecordSuccessAsync_ClearsErrorMessage_WhenPreviouslyFailed()
    {
        // Arrange
        SeedStatus(MakeTimeBased(SyncTypes.Metar, errorMessage: "Connection timeout"));

        // Act
        await _service.RecordSuccessAsync(SyncTypes.Metar);

        // Assert
        var status = await _dbContext.DataSyncStatuses.FindAsync(SyncTypes.Metar);
        status!.LastErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task RecordSuccessAsync_UnknownSyncType_LogsWarningAndReturns()
    {
        // Act — no seeded row for "UnknownType"
        var act = () => _service.RecordSuccessAsync("UnknownType");

        // Assert
        await act.Should().NotThrowAsync();
        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("not found")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task RecordSuccessAsync_DbException_LogsWarningAndDoesNotThrow()
    {
        // Arrange — seed then dispose to force DB error
        SeedStatus(MakeTimeBased(SyncTypes.Metar));
        await _dbContext.DisposeAsync();

        // Act
        var act = () => _service.RecordSuccessAsync(SyncTypes.Metar);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region RecordFailureAsync

    [Fact]
    public async Task RecordFailureAsync_ValidSyncType_UpdatesFailureFields()
    {
        // Arrange
        SeedStatus(MakeTimeBased(SyncTypes.Metar, lastSync: DateTime.UtcNow.AddMinutes(-10)));

        // Act
        await _service.RecordFailureAsync(SyncTypes.Metar, "Connection refused");

        // Assert
        var status = await _dbContext.DataSyncStatuses.FindAsync(SyncTypes.Metar);
        status!.LastSyncSucceeded.Should().BeFalse();
        status.ConsecutiveFailures.Should().Be(1);
        status.LastAttemptedSyncUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        status.LastErrorMessage.Should().Be("Connection refused");
    }

    [Fact]
    public async Task RecordFailureAsync_IncrementsConsecutiveFailures()
    {
        // Arrange
        SeedStatus(MakeTimeBased(SyncTypes.Metar, consecutiveFailures: 3));

        // Act
        await _service.RecordFailureAsync(SyncTypes.Metar, "error");

        // Assert
        var status = await _dbContext.DataSyncStatuses.FindAsync(SyncTypes.Metar);
        status!.ConsecutiveFailures.Should().Be(4);
    }

    [Fact]
    public async Task RecordFailureAsync_TruncatesErrorMessage_At2000Chars()
    {
        // Arrange
        SeedStatus(MakeTimeBased(SyncTypes.Metar));
        var longMessage = new string('x', 3000);

        // Act
        await _service.RecordFailureAsync(SyncTypes.Metar, longMessage);

        // Assert
        var status = await _dbContext.DataSyncStatuses.FindAsync(SyncTypes.Metar);
        status!.LastErrorMessage!.Length.Should().Be(2000);
    }

    [Fact]
    public async Task RecordFailureAsync_ShortErrorMessage_NotTruncated()
    {
        // Arrange
        SeedStatus(MakeTimeBased(SyncTypes.Metar));
        var shortMessage = new string('x', 100);

        // Act
        await _service.RecordFailureAsync(SyncTypes.Metar, shortMessage);

        // Assert
        var status = await _dbContext.DataSyncStatuses.FindAsync(SyncTypes.Metar);
        status!.LastErrorMessage!.Length.Should().Be(100);
    }

    [Fact]
    public async Task RecordFailureAsync_UnknownSyncType_LogsWarningAndReturns()
    {
        // Act
        var act = () => _service.RecordFailureAsync("UnknownType", "error");

        // Assert
        await act.Should().NotThrowAsync();
        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("not found")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task RecordFailureAsync_DbException_LogsWarningAndDoesNotThrow()
    {
        // Arrange
        SeedStatus(MakeTimeBased(SyncTypes.Metar));
        await _dbContext.DisposeAsync();

        // Act
        var act = () => _service.RecordFailureAsync(SyncTypes.Metar, "error");

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region GetAllFreshness — TimeBased

    [Fact]
    public async Task GetAllFreshness_TimeBased_NeverSynced_ReturnsCritical()
    {
        // Arrange
        SeedStatus(MakeTimeBased("Metar", threshold: 50, lastSync: null));

        // Act
        var results = await _service.GetAllFreshnessAsync();

        // Assert
        var result = results.Single();
        result.SyncType.Should().Be("Metar");
        result.Severity.Should().Be("critical");
        result.IsFresh.Should().BeFalse();
        result.AgeMinutes.Should().BeNull();
        result.Message.Should().Contain("never been synced");
    }

    [Fact]
    public async Task GetAllFreshness_TimeBased_Fresh_ReturnsNone()
    {
        // Arrange — age = threshold * 0.5
        SeedStatus(MakeTimeBased("Metar", threshold: 100, lastSync: DateTime.UtcNow.AddMinutes(-50)));

        // Act
        var results = await _service.GetAllFreshnessAsync();

        // Assert
        var result = results.Single();
        result.Severity.Should().Be("none");
        result.IsFresh.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllFreshness_TimeBased_ApproachingStaleness_ReturnsInfo()
    {
        // Arrange — age = threshold * 1.2
        SeedStatus(MakeTimeBased("Metar", threshold: 100, lastSync: DateTime.UtcNow.AddMinutes(-120)));

        // Act
        var results = await _service.GetAllFreshnessAsync();

        // Assert
        var result = results.Single();
        result.Severity.Should().Be("info");
        result.IsFresh.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllFreshness_TimeBased_Stale_ReturnsWarning()
    {
        // Arrange — age = threshold * 1.7
        SeedStatus(MakeTimeBased("Metar", threshold: 100, lastSync: DateTime.UtcNow.AddMinutes(-170)));

        // Act
        var results = await _service.GetAllFreshnessAsync();

        // Assert
        var result = results.Single();
        result.Severity.Should().Be("warning");
        result.IsFresh.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllFreshness_TimeBased_CriticallyStale_ReturnsCritical()
    {
        // Arrange — age = threshold * 2.5
        SeedStatus(MakeTimeBased("Metar", threshold: 100, lastSync: DateTime.UtcNow.AddMinutes(-250)));

        // Act
        var results = await _service.GetAllFreshnessAsync();

        // Assert
        var result = results.Single();
        result.Severity.Should().Be("critical");
        result.IsFresh.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllFreshness_TimeBased_ExactThreshold_ReturnsInfo()
    {
        // Arrange — ratio = 1.0 exactly (plus tiny buffer for execution time)
        // Code: ratio < 1.0 → none; else if ratio < 1.5 → info
        // At ratio = 1.0, condition < 1.0 is false → falls into info
        SeedStatus(MakeTimeBased("Metar", threshold: 1000, lastSync: DateTime.UtcNow.AddMinutes(-1001)));

        // Act
        var results = await _service.GetAllFreshnessAsync();

        // Assert
        results.Single().Severity.Should().Be("info");
    }

    [Fact]
    public async Task GetAllFreshness_TimeBased_ExactWarningBoundary_ReturnsWarning()
    {
        // Arrange — ratio = 1.5 (plus tiny buffer)
        // Code: ratio < 1.5 → info; else if ratio < 2.0 → warning
        // At ratio = 1.5, condition < 1.5 is false → falls into warning
        SeedStatus(MakeTimeBased("Metar", threshold: 1000, lastSync: DateTime.UtcNow.AddMinutes(-1501)));

        // Act
        var results = await _service.GetAllFreshnessAsync();

        // Assert
        results.Single().Severity.Should().Be("warning");
    }

    [Fact]
    public async Task GetAllFreshness_TimeBased_ExactCriticalBoundary_ReturnsCritical()
    {
        // Arrange — ratio = 2.0 (plus tiny buffer)
        // Code: ratio < 2.0 → warning; else → critical
        // At ratio = 2.0, condition < 2.0 is false → falls into critical
        SeedStatus(MakeTimeBased("Metar", threshold: 1000, lastSync: DateTime.UtcNow.AddMinutes(-2001)));

        // Act
        var results = await _service.GetAllFreshnessAsync();

        // Assert
        results.Single().Severity.Should().Be("critical");
    }

    [Fact]
    public async Task GetAllFreshness_TimeBased_NullThreshold_DefaultsTo60Minutes()
    {
        // Arrange — null threshold, age < 60 should be fresh
        var status = MakeTimeBased("Metar", lastSync: DateTime.UtcNow.AddMinutes(-30));
        status.StalenessThresholdMinutes = null;
        SeedStatus(status);

        // Act
        var results = await _service.GetAllFreshnessAsync();

        // Assert
        var result = results.Single();
        result.ThresholdMinutes.Should().Be(60);
        result.Severity.Should().Be("none");
        result.IsFresh.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllFreshness_TimeBased_AgeMinutesRoundedToOneDecimal()
    {
        // Arrange
        SeedStatus(MakeTimeBased("Metar", threshold: 200, lastSync: DateTime.UtcNow.AddMinutes(-50)));

        // Act
        var results = await _service.GetAllFreshnessAsync();

        // Assert
        var result = results.Single();
        result.AgeMinutes.Should().NotBeNull();
        result.AgeMinutes!.Value.Should().Be(Math.Round(result.AgeMinutes.Value, 1));
    }

    [Fact]
    public async Task GetAllFreshness_TimeBased_PopulatesAlertTrackingFields()
    {
        // Arrange
        var alertTime = DateTime.UtcNow.AddHours(-1);
        SeedStatus(MakeTimeBased("Metar", threshold: 100,
            lastSync: DateTime.UtcNow.AddMinutes(-50),
            lastAlertSent: alertTime, lastAlertSeverity: "warning"));

        // Act
        var results = await _service.GetAllFreshnessAsync();

        // Assert
        var result = results.Single();
        result.LastAlertSentUtc.Should().BeCloseTo(alertTime, TimeSpan.FromSeconds(1));
        result.LastAlertSeverity.Should().Be("warning");
    }

    #endregion

    #region GetAllFreshness — CycleBased

    [Fact]
    public async Task GetAllFreshness_CycleBased_NoCycleFound_ReturnsWarning()
    {
        // Arrange — no FaaPublicationCycle seeded
        SeedStatus(MakeCycleBased("Airport", "NasrSubscription_Airport",
            lastSync: DateTime.UtcNow.AddDays(-1)));

        // Act
        var results = await _service.GetAllFreshnessAsync();

        // Assert
        var result = results.Single();
        result.Severity.Should().Be("warning");
        result.IsFresh.Should().BeFalse();
        result.Message.Should().Contain("no publication cycle found");
    }

    [Fact]
    public async Task GetAllFreshness_CycleBased_NeverSynced_ReturnsCritical()
    {
        // Arrange
        SeedStatus(MakeCycleBased("Airport", "ChartSupplement", lastSync: null));
        SeedCycles(new FaaPublicationCycle
        {
            Id = 1,
            PublicationType = PublicationType.ChartSupplement,
            CycleLengthDays = 1000,
            KnownValidDate = DateTime.UtcNow.AddDays(-5)
        });

        // Act
        var results = await _service.GetAllFreshnessAsync();

        // Assert
        var result = results.Single();
        result.Severity.Should().Be("critical");
        result.IsFresh.Should().BeFalse();
        result.CurrentCycleDate.Should().NotBeNull();
        result.Message.Should().Contain("never been synced");
    }

    [Fact]
    public async Task GetAllFreshness_CycleBased_SyncedAfterCycleDate_ReturnsFresh()
    {
        // Arrange — cycle started 5 days ago, synced 2 days ago → after cycle
        SeedStatus(MakeCycleBased("Airport", "ChartSupplement",
            lastSync: DateTime.UtcNow.AddDays(-2)));
        SeedCycles(new FaaPublicationCycle
        {
            Id = 1,
            PublicationType = PublicationType.ChartSupplement,
            CycleLengthDays = 1000,
            KnownValidDate = DateTime.UtcNow.AddDays(-5)
        });

        // Act
        var results = await _service.GetAllFreshnessAsync();

        // Assert
        var result = results.Single();
        result.Severity.Should().Be("none");
        result.IsFresh.Should().BeTrue();
        result.DaysPastCycleWithoutUpdate.Should().Be(0);
    }

    [Fact]
    public async Task GetAllFreshness_CycleBased_LessThanOneDayPastCycle_ReturnsInfo()
    {
        // Arrange — cycle started ~12 hours ago, last sync before cycle
        SeedStatus(MakeCycleBased("Airport", "ChartSupplement",
            lastSync: DateTime.UtcNow.AddDays(-5)));
        SeedCycles(new FaaPublicationCycle
        {
            Id = 1,
            PublicationType = PublicationType.ChartSupplement,
            CycleLengthDays = 1000,
            KnownValidDate = DateTime.UtcNow.AddHours(-12)
        });

        // Act
        var results = await _service.GetAllFreshnessAsync();

        // Assert
        var result = results.Single();
        result.Severity.Should().Be("info");
        result.IsFresh.Should().BeFalse();
        result.DaysPastCycleWithoutUpdate.Should().BeInRange(0.4, 0.6);
    }

    [Fact]
    public async Task GetAllFreshness_CycleBased_OneToDaysPastCycle_ReturnsWarning()
    {
        // Arrange — cycle started ~1.5 days ago, last sync before cycle
        SeedStatus(MakeCycleBased("Airport", "ChartSupplement",
            lastSync: DateTime.UtcNow.AddDays(-10)));
        SeedCycles(new FaaPublicationCycle
        {
            Id = 1,
            PublicationType = PublicationType.ChartSupplement,
            CycleLengthDays = 1000,
            KnownValidDate = DateTime.UtcNow.AddHours(-36)
        });

        // Act
        var results = await _service.GetAllFreshnessAsync();

        // Assert
        var result = results.Single();
        result.Severity.Should().Be("warning");
        result.IsFresh.Should().BeFalse();
        result.DaysPastCycleWithoutUpdate.Should().BeInRange(1.4, 1.6);
    }

    [Fact]
    public async Task GetAllFreshness_CycleBased_TwoPlusDaysPastCycle_ReturnsCritical()
    {
        // Arrange — cycle started 3 days ago, last sync before cycle
        SeedStatus(MakeCycleBased("Airport", "ChartSupplement",
            lastSync: DateTime.UtcNow.AddDays(-10)));
        SeedCycles(new FaaPublicationCycle
        {
            Id = 1,
            PublicationType = PublicationType.ChartSupplement,
            CycleLengthDays = 1000,
            KnownValidDate = DateTime.UtcNow.AddDays(-3)
        });

        // Act
        var results = await _service.GetAllFreshnessAsync();

        // Assert
        var result = results.Single();
        result.Severity.Should().Be("critical");
        result.IsFresh.Should().BeFalse();
        result.DaysPastCycleWithoutUpdate.Should().BeInRange(2.9, 3.1);
    }

    [Fact]
    public async Task GetAllFreshness_CycleBased_ExactlyOneDayBoundary_ReturnsWarning()
    {
        // Arrange — cycle started just over 1 day ago (boundary: daysPast >= 1 → warning)
        SeedStatus(MakeCycleBased("Airport", "ChartSupplement",
            lastSync: DateTime.UtcNow.AddDays(-10)));
        SeedCycles(new FaaPublicationCycle
        {
            Id = 1,
            PublicationType = PublicationType.ChartSupplement,
            CycleLengthDays = 1000,
            KnownValidDate = DateTime.UtcNow.AddDays(-1).AddMinutes(-5)
        });

        // Act
        var results = await _service.GetAllFreshnessAsync();

        // Assert
        results.Single().Severity.Should().Be("warning");
    }

    [Fact]
    public async Task GetAllFreshness_CycleBased_ExactlyTwoDayBoundary_ReturnsCritical()
    {
        // Arrange — cycle started just over 2 days ago (boundary: daysPast >= 2 → critical)
        SeedStatus(MakeCycleBased("Airport", "ChartSupplement",
            lastSync: DateTime.UtcNow.AddDays(-10)));
        SeedCycles(new FaaPublicationCycle
        {
            Id = 1,
            PublicationType = PublicationType.ChartSupplement,
            CycleLengthDays = 1000,
            KnownValidDate = DateTime.UtcNow.AddDays(-2).AddMinutes(-5)
        });

        // Act
        var results = await _service.GetAllFreshnessAsync();

        // Assert
        results.Single().Severity.Should().Be("critical");
    }

    [Fact]
    public async Task GetAllFreshness_CycleBased_PopulatesCurrentCycleDate()
    {
        // Arrange
        var knownDate = DateTime.UtcNow.AddDays(-5);
        SeedStatus(MakeCycleBased("Airport", "ChartSupplement",
            lastSync: DateTime.UtcNow));
        SeedCycles(new FaaPublicationCycle
        {
            Id = 1,
            PublicationType = PublicationType.ChartSupplement,
            CycleLengthDays = 1000,
            KnownValidDate = knownDate
        });

        // Act
        var results = await _service.GetAllFreshnessAsync();

        // Assert — cycle date should equal knownDate (since <1000 days elapsed)
        var result = results.Single();
        result.CurrentCycleDate.Should().BeCloseTo(knownDate, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetAllFreshness_CycleBased_NullPublicationType_NoCycleMatch()
    {
        // Arrange — PublicationType is null → treated as no cycle found
        SeedStatus(MakeCycleBased("Airport", publicationType: null,
            lastSync: DateTime.UtcNow.AddDays(-1)));
        SeedCycles(new FaaPublicationCycle
        {
            Id = 1,
            PublicationType = PublicationType.ChartSupplement,
            CycleLengthDays = 56,
            KnownValidDate = DateTime.UtcNow.AddDays(-5)
        });

        // Act
        var results = await _service.GetAllFreshnessAsync();

        // Assert
        var result = results.Single();
        result.Severity.Should().Be("warning");
        result.Message.Should().Contain("no publication cycle found");
    }

    #endregion

    #region UpdateAlertStateAsync / ClearAlertStateAsync

    [Fact]
    public async Task UpdateAlertStateAsync_ValidType_SetsAlertFields()
    {
        // Arrange
        SeedStatus(MakeTimeBased(SyncTypes.Metar));

        // Act
        await _service.UpdateAlertStateAsync(SyncTypes.Metar, "warning");

        // Assert
        var status = await _dbContext.DataSyncStatuses.FindAsync(SyncTypes.Metar);
        status!.LastAlertSentUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        status.LastAlertSeverity.Should().Be("warning");
    }

    [Fact]
    public async Task UpdateAlertStateAsync_UnknownType_ReturnsSilently()
    {
        // Act
        var act = () => _service.UpdateAlertStateAsync("UnknownType", "warning");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ClearAlertStateAsync_ValidType_NullifiesAlertFields()
    {
        // Arrange
        SeedStatus(MakeTimeBased(SyncTypes.Metar,
            lastAlertSent: DateTime.UtcNow, lastAlertSeverity: "warning"));

        // Act
        await _service.ClearAlertStateAsync(SyncTypes.Metar);

        // Assert
        var status = await _dbContext.DataSyncStatuses.FindAsync(SyncTypes.Metar);
        status!.LastAlertSentUtc.Should().BeNull();
        status.LastAlertSeverity.Should().BeNull();
    }

    [Fact]
    public async Task ClearAlertStateAsync_UnknownType_ReturnsSilently()
    {
        // Act
        var act = () => _service.ClearAlertStateAsync("UnknownType");

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region GetAllFreshness — Routing

    [Fact]
    public async Task GetAllFreshness_MixedModes_EvaluatesCorrectly()
    {
        // Arrange — one time-based (fresh) and one cycle-based (stale, no cycle)
        SeedStatus(
            MakeTimeBased("Metar", threshold: 100, lastSync: DateTime.UtcNow.AddMinutes(-10)),
            MakeCycleBased("Airport", "ChartSupplement", lastSync: DateTime.UtcNow.AddDays(-10)));
        // No cycle seeded → cycle-based gets "warning" (no cycle found)

        // Act
        var results = await _service.GetAllFreshnessAsync();

        // Assert
        results.Should().HaveCount(2);
        results.Single(r => r.SyncType == "Metar").Severity.Should().Be("none");
        results.Single(r => r.SyncType == "Airport").Severity.Should().Be("warning");
    }

    #endregion
}

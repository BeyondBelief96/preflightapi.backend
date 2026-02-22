using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos.Notam;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Services.NotamServices;
using PreflightApi.Infrastructure.Settings;
using Xunit;

namespace PreflightApi.Tests.NotamTests;

public class NotamDeltaSyncCronServiceTests : IDisposable
{
    private readonly PreflightApiDbContext _dbContext;
    private readonly INmsApiClient _nmsApiClient;
    private readonly NotamDeltaSyncCronService _service;

    public NotamDeltaSyncCronServiceTests()
    {
        var options = new DbContextOptionsBuilder<PreflightApiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PreflightApiDbContext(options);
        _nmsApiClient = Substitute.For<INmsApiClient>();

        var settings = Options.Create(new NmsSettings
        {
            DeltaSyncIntervalMinutes = 3
        });

        var logger = Substitute.For<ILogger<NotamDeltaSyncCronService>>();

        var telemetry = Substitute.For<ISyncTelemetryService>();
        _service = new NotamDeltaSyncCronService(_nmsApiClient, _dbContext, settings, logger, telemetry);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task SyncDeltaAsync_ShouldInsertNewNotams()
    {
        // Arrange
        var notams = new List<NotamDto>
        {
            CreateNotamDto("0000000000000001", "DFW", "KDFW"),
            CreateNotamDto("0000000000000002", "AUS", "KAUS")
        };
        _nmsApiClient.GetNotamsByLastUpdatedDateAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(notams);

        // Act
        await _service.SyncDeltaAsync();

        // Assert
        var dbNotams = await _dbContext.Notams.ToListAsync();
        dbNotams.Should().HaveCount(2);
        dbNotams.Should().Contain(n => n.NmsId == "0000000000000001");
        dbNotams.Should().Contain(n => n.NmsId == "0000000000000002");
    }

    [Fact]
    public async Task SyncDeltaAsync_ShouldUpdateExistingNotams()
    {
        // Arrange — seed existing NOTAM
        _dbContext.Notams.Add(new Domain.Entities.Notam
        {
            NmsId = "0000000000000001",
            Location = "DFW",
            IcaoLocation = "KDFW",
            Text = "Old text",
            SyncedAt = DateTime.UtcNow.AddMinutes(-10),
            FeatureJson = "{}"
        });
        await _dbContext.SaveChangesAsync();

        var updatedNotams = new List<NotamDto>
        {
            CreateNotamDto("0000000000000001", "DFW", "KDFW", text: "Updated text")
        };
        _nmsApiClient.GetNotamsByLastUpdatedDateAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(updatedNotams);

        // Act
        await _service.SyncDeltaAsync();

        // Assert
        var dbNotam = await _dbContext.Notams.FirstAsync(n => n.NmsId == "0000000000000001");
        dbNotam.Text.Should().Be("Updated text");
        dbNotam.SyncedAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task SyncDeltaAsync_ShouldHandleMixOfNewAndExisting()
    {
        // Arrange
        _dbContext.Notams.Add(new Domain.Entities.Notam
        {
            NmsId = "0000000000000001",
            Location = "DFW",
            IcaoLocation = "KDFW",
            Text = "Existing",
            SyncedAt = DateTime.UtcNow.AddMinutes(-10),
            FeatureJson = "{}"
        });
        await _dbContext.SaveChangesAsync();

        var notams = new List<NotamDto>
        {
            CreateNotamDto("0000000000000001", "DFW", "KDFW", text: "Updated"),
            CreateNotamDto("0000000000000002", "AUS", "KAUS", text: "New one")
        };
        _nmsApiClient.GetNotamsByLastUpdatedDateAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(notams);

        // Act
        await _service.SyncDeltaAsync();

        // Assert
        var dbNotams = await _dbContext.Notams.ToListAsync();
        dbNotams.Should().HaveCount(2);
    }

    [Fact]
    public async Task SyncDeltaAsync_ShouldDoNothing_WhenNoUpdates()
    {
        // Arrange
        _nmsApiClient.GetNotamsByLastUpdatedDateAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<NotamDto>());

        // Act
        await _service.SyncDeltaAsync();

        // Assert
        var dbNotams = await _dbContext.Notams.ToListAsync();
        dbNotams.Should().BeEmpty();
    }

    [Fact]
    public void ExtractNmsId_ShouldReturnTopLevelId()
    {
        var dto = new NotamDto { Id = "1234567890123456" };
        NotamDeltaSyncCronService.ExtractNmsId(dto).Should().Be("1234567890123456");
    }

    [Fact]
    public void ExtractNmsId_ShouldFallbackToNestedId()
    {
        var dto = new NotamDto
        {
            Properties = new NotamPropertiesDto
            {
                CoreNotamData = new CoreNotamDataDto
                {
                    Notam = new NotamDetailDto { Id = "9876543210123456" }
                }
            }
        };
        NotamDeltaSyncCronService.ExtractNmsId(dto).Should().Be("9876543210123456");
    }

    [Fact]
    public void ExtractNmsId_ShouldReturnNull_WhenNoIdAvailable()
    {
        var dto = new NotamDto();
        NotamDeltaSyncCronService.ExtractNmsId(dto).Should().BeNull();
    }

    [Fact]
    public async Task PurgeExpiredAsync_ShouldRemoveExpiredNotams()
    {
        // Arrange — expired NOTAM (effective_end in the past)
        _dbContext.Notams.Add(new Domain.Entities.Notam
        {
            NmsId = "EXPIRED_001",
            Location = "DFW",
            NotamType = "N",
            EffectiveEnd = DateTime.UtcNow.AddHours(-1),
            SyncedAt = DateTime.UtcNow,
            FeatureJson = "{}"
        });
        _dbContext.Notams.Add(new Domain.Entities.Notam
        {
            NmsId = "ACTIVE_001",
            Location = "AUS",
            NotamType = "N",
            EffectiveEnd = DateTime.UtcNow.AddDays(30),
            SyncedAt = DateTime.UtcNow,
            FeatureJson = "{}"
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var purgedCount = await _service.PurgeExpiredAsync();

        // Assert
        purgedCount.Should().Be(1);
        var remaining = await _dbContext.Notams.ToListAsync();
        remaining.Should().HaveCount(1);
        remaining[0].NmsId.Should().Be("ACTIVE_001");
    }

    [Fact]
    public async Task PurgeExpiredAsync_ShouldNotRemovePermanentNotams()
    {
        // Arrange — permanent NOTAMs have null effectiveEnd ("PERM")
        _dbContext.Notams.Add(new Domain.Entities.Notam
        {
            NmsId = "PERMANENT_001",
            Location = "AUS",
            NotamType = "R",
            EffectiveEnd = null,
            SyncedAt = DateTime.UtcNow,
            FeatureJson = "{}"
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var purgedCount = await _service.PurgeExpiredAsync();

        // Assert
        purgedCount.Should().Be(0);
        var remaining = await _dbContext.Notams.ToListAsync();
        remaining.Should().HaveCount(1);
    }

    [Fact]
    public async Task PurgeExpiredAsync_ShouldRemoveCancelledNotamEvenWithFutureEffectiveEnd()
    {
        // Arrange — cancelled (cancelationDate in past) even though effectiveEnd is still future
        _dbContext.Notams.Add(new Domain.Entities.Notam
        {
            NmsId = "CANCELLED_FUTURE",
            Location = "DFW",
            NotamType = "N",
            CancelationDate = DateTime.UtcNow.AddHours(-1),
            EffectiveEnd = DateTime.UtcNow.AddDays(5),
            SyncedAt = DateTime.UtcNow,
            FeatureJson = "{}"
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var purgedCount = await _service.PurgeExpiredAsync();

        // Assert — purged because cancellation has taken effect
        purgedCount.Should().Be(1);
        var remaining = await _dbContext.Notams.ToListAsync();
        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task PurgeExpiredAsync_ShouldNotRemoveNotamWithFutureCancelationDate()
    {
        // Arrange — cancellation date is in the future (hasn't taken effect yet)
        _dbContext.Notams.Add(new Domain.Entities.Notam
        {
            NmsId = "CANCEL_PENDING",
            Location = "DFW",
            NotamType = "N",
            CancelationDate = DateTime.UtcNow.AddHours(1),
            EffectiveEnd = DateTime.UtcNow.AddDays(5),
            SyncedAt = DateTime.UtcNow,
            FeatureJson = "{}"
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var purgedCount = await _service.PurgeExpiredAsync();

        // Assert — not purged, cancellation hasn't taken effect
        purgedCount.Should().Be(0);
        var remaining = await _dbContext.Notams.ToListAsync();
        remaining.Should().HaveCount(1);
    }

    [Fact]
    public async Task PurgeExpiredAsync_ShouldRemoveCancelledNotamWithPastEffectiveEnd()
    {
        // Arrange — cancelled AND effectiveEnd has passed
        _dbContext.Notams.AddRange(
            new Domain.Entities.Notam
            {
                NmsId = "CANCELLED_EXPIRED",
                NotamType = "N",
                CancelationDate = DateTime.UtcNow.AddDays(-3),
                EffectiveEnd = DateTime.UtcNow.AddDays(-1),
                SyncedAt = DateTime.UtcNow, FeatureJson = "{}"
            },
            new Domain.Entities.Notam
            {
                NmsId = "ACTIVE_001",
                NotamType = "N",
                EffectiveEnd = DateTime.UtcNow.AddDays(30),
                SyncedAt = DateTime.UtcNow, FeatureJson = "{}"
            }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var purgedCount = await _service.PurgeExpiredAsync();

        // Assert
        purgedCount.Should().Be(1);
        var remaining = await _dbContext.Notams.ToListAsync();
        remaining.Should().HaveCount(1);
        remaining[0].NmsId.Should().Be("ACTIVE_001");
    }

    private static NotamDto CreateNotamDto(string id, string location, string icaoLocation, string text = "Test NOTAM")
    {
        return new NotamDto
        {
            Type = "Feature",
            Id = id,
            Properties = new NotamPropertiesDto
            {
                CoreNotamData = new CoreNotamDataDto
                {
                    Notam = new NotamDetailDto
                    {
                        Id = id,
                        Number = "01/001",
                        Location = location,
                        IcaoLocation = icaoLocation,
                        Classification = "DOMESTIC",
                        Type = "N",
                        Text = text,
                        EffectiveStart = DateTime.UtcNow.AddHours(-1).ToString("O"),
                        LastUpdated = DateTime.UtcNow.ToString("O")
                    }
                }
            }
        };
    }
}

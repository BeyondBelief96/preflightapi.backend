using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos.Notam;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Services.NotamServices;
using Xunit;

namespace PreflightApi.Tests.NotamTests;

public class NotamInitialLoadCronServiceTests : IDisposable
{
    private readonly PreflightApiDbContext _dbContext;
    private readonly INmsApiClient _nmsApiClient;
    private readonly NotamInitialLoadCronService _service;

    public NotamInitialLoadCronServiceTests()
    {
        var options = new DbContextOptionsBuilder<PreflightApiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PreflightApiDbContext(options);
        _nmsApiClient = Substitute.For<INmsApiClient>();

        var logger = Substitute.For<ILogger<NotamInitialLoadCronService>>();

        _service = new NotamInitialLoadCronService(_nmsApiClient, _dbContext, logger);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task LoadAllClassificationsAsync_ShouldInsertNewNotams()
    {
        // Arrange
        var notams = new List<NotamDto>
        {
            CreateNotamDto("NMS_001", "DFW", "KDFW"),
            CreateNotamDto("NMS_002", "AUS", "KAUS")
        };
        _nmsApiClient.GetAllNotamsInitialLoadAsync(Arg.Any<CancellationToken>())
            .Returns(notams);

        // Act
        await _service.LoadAllClassificationsAsync();

        // Assert
        var dbNotams = await _dbContext.Notams.ToListAsync();
        dbNotams.Should().HaveCount(2);
        dbNotams.Should().Contain(n => n.NmsId == "NMS_001" && n.Location == "DFW");
        dbNotams.Should().Contain(n => n.NmsId == "NMS_002" && n.Location == "AUS");
    }

    [Fact]
    public async Task LoadAllClassificationsAsync_ShouldUpdateExistingNotams()
    {
        // Arrange — seed existing NOTAM
        _dbContext.Notams.Add(new Domain.Entities.Notam
        {
            NmsId = "NMS_001",
            Location = "DFW",
            IcaoLocation = "KDFW",
            Text = "Old text",
            SyncedAt = DateTime.UtcNow.AddHours(-25),
            FeatureJson = "{}"
        });
        await _dbContext.SaveChangesAsync();

        var notams = new List<NotamDto>
        {
            CreateNotamDto("NMS_001", "DFW", "KDFW", text: "New text from initial load")
        };
        _nmsApiClient.GetAllNotamsInitialLoadAsync(Arg.Any<CancellationToken>())
            .Returns(notams);

        // Act
        await _service.LoadAllClassificationsAsync();

        // Assert
        var dbNotam = await _dbContext.Notams.FirstAsync(n => n.NmsId == "NMS_001");
        dbNotam.Text.Should().Be("New text from initial load");
        dbNotam.SyncedAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task LoadAllClassificationsAsync_ShouldPurgeCancelledNotamWithPastCancelationDate()
    {
        // Arrange — cancelled (cancelationDate in past) even though effectiveEnd is future
        _dbContext.Notams.Add(new Domain.Entities.Notam
        {
            NmsId = "CANCELLED_PAST",
            Location = "DFW",
            NotamType = "N",
            CancelationDate = DateTime.UtcNow.AddHours(-1),
            EffectiveEnd = DateTime.UtcNow.AddDays(10),
            SyncedAt = DateTime.UtcNow.AddHours(-25),
            FeatureJson = "{}"
        });
        await _dbContext.SaveChangesAsync();

        _nmsApiClient.GetAllNotamsInitialLoadAsync(Arg.Any<CancellationToken>())
            .Returns(new List<NotamDto> { CreateNotamDto("NMS_ACTIVE", "AUS", "KAUS") });

        // Act
        await _service.LoadAllClassificationsAsync();

        // Assert — cancelled NOTAM purged, only the new active one remains
        var remaining = await _dbContext.Notams.ToListAsync();
        remaining.Should().HaveCount(1);
        remaining[0].NmsId.Should().Be("NMS_ACTIVE");
    }

    [Fact]
    public async Task LoadAllClassificationsAsync_ShouldPurgeExpiredNotamsAtEnd()
    {
        // Arrange — pre-existing expired NOTAM
        _dbContext.Notams.Add(new Domain.Entities.Notam
        {
            NmsId = "EXPIRED_OLD",
            Location = "DFW",
            NotamType = "N",
            EffectiveEnd = DateTime.UtcNow.AddHours(-2), // Expired
            SyncedAt = DateTime.UtcNow.AddHours(-25),
            FeatureJson = "{}"
        });
        await _dbContext.SaveChangesAsync();

        _nmsApiClient.GetAllNotamsInitialLoadAsync(Arg.Any<CancellationToken>())
            .Returns(new List<NotamDto> { CreateNotamDto("NMS_ACTIVE", "AUS", "KAUS") });

        // Act
        await _service.LoadAllClassificationsAsync();

        // Assert
        var remaining = await _dbContext.Notams.ToListAsync();
        remaining.Should().HaveCount(1);
        remaining[0].NmsId.Should().Be("NMS_ACTIVE");
    }

    [Fact]
    public async Task LoadAllClassificationsAsync_ShouldNotPurgePermanentNotams()
    {
        // Arrange — permanent NOTAM (no effective end)
        _dbContext.Notams.Add(new Domain.Entities.Notam
        {
            NmsId = "PERMANENT_001",
            Location = "DFW",
            NotamType = "R",
            EffectiveEnd = null, // PERM
            SyncedAt = DateTime.UtcNow.AddHours(-25),
            FeatureJson = "{}"
        });
        await _dbContext.SaveChangesAsync();

        _nmsApiClient.GetAllNotamsInitialLoadAsync(Arg.Any<CancellationToken>())
            .Returns(new List<NotamDto>());

        // Act
        await _service.LoadAllClassificationsAsync();

        // Assert — permanent NOTAM should survive the purge
        var remaining = await _dbContext.Notams.ToListAsync();
        remaining.Should().HaveCount(1);
        remaining[0].NmsId.Should().Be("PERMANENT_001");
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
                        EffectiveEnd = DateTime.UtcNow.AddDays(30).ToString("O"),
                        LastUpdated = DateTime.UtcNow.ToString("O")
                    }
                }
            }
        };
    }
}

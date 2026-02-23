using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos.Notam;
using PreflightApi.Infrastructure.Services.NotamServices;
using PreflightApi.Infrastructure.Settings;
using Xunit;

namespace PreflightApi.Tests.NotamTests;

/// <summary>
/// Tests for NotamService route queries — order independence, dedup, and edge cases.
/// </summary>
public class NotamServiceRouteTests : IDisposable
{
    private readonly PreflightApiDbContext _dbContext;
    private readonly NotamService _service;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public NotamServiceRouteTests()
    {
        var options = new DbContextOptionsBuilder<PreflightApiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PreflightApiDbContext(options);

        var settings = Options.Create(new NmsSettings
        {
            CacheDurationMinutes = 5,
            DefaultRouteCorridorRadiusNm = 25
        });
        var logger = Substitute.For<ILogger<NotamService>>();

        _service = new NotamService(_dbContext, settings, logger);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region Airport Identifier Route Tests

    [Fact]
    public async Task GetNotamsForRouteAsync_ShouldReturnSameResults_RegardlessOfAirportOrder()
    {
        // Arrange — seed NOTAMs for two different airports
        SeedNotams(
            CreateNotamEntity("0000000000000001", "DFW", "KDFW"),
            CreateNotamEntity("0000000000000002", "DFW", "KDFW"),
            CreateNotamEntity("0000000000000003", "AUS", "KAUS"),
            CreateNotamEntity("0000000000000004", "AUS", "KAUS")
        );

        var requestAB = new NotamQueryByRouteRequest
        {
            AirportIdentifiers = ["KDFW", "KAUS"]
        };

        var requestBA = new NotamQueryByRouteRequest
        {
            AirportIdentifiers = ["KAUS", "KDFW"]
        };

        // Act
        var resultAB = await _service.GetNotamsForRouteAsync(requestAB);
        var resultBA = await _service.GetNotamsForRouteAsync(requestBA);

        // Assert — same count and same NOTAM IDs regardless of order
        resultAB.TotalCount.Should().Be(4);
        resultBA.TotalCount.Should().Be(4);

        var idsAB = resultAB.Notams.Select(n => n.Id).OrderBy(id => id).ToList();
        var idsBA = resultBA.Notams.Select(n => n.Id).OrderBy(id => id).ToList();
        idsAB.Should().BeEquivalentTo(idsBA);
    }

    [Fact]
    public async Task GetNotamsForRouteAsync_ShouldReturnSameResults_RegardlessOfAirportOrder_WithOverlap()
    {
        // Arrange — one NOTAM visible to both airports (same Location matches both queries)
        SeedNotams(
            CreateNotamEntity("0000000000000001", "DFW", "KDFW"),
            CreateNotamEntity("0000000000000002", "AUS", "KAUS"),
            CreateNotamEntity("0000000000000003", "DFW", "KDFW") // second DFW NOTAM
        );

        var requestAB = new NotamQueryByRouteRequest
        {
            AirportIdentifiers = ["KDFW", "KAUS"]
        };

        var requestBA = new NotamQueryByRouteRequest
        {
            AirportIdentifiers = ["KAUS", "KDFW"]
        };

        // Act
        var resultAB = await _service.GetNotamsForRouteAsync(requestAB);
        var resultBA = await _service.GetNotamsForRouteAsync(requestBA);

        // Assert — dedup ensures same count both ways
        resultAB.TotalCount.Should().Be(3);
        resultBA.TotalCount.Should().Be(3);

        var idsAB = resultAB.Notams.Select(n => n.Id).OrderBy(id => id).ToList();
        var idsBA = resultBA.Notams.Select(n => n.Id).OrderBy(id => id).ToList();
        idsAB.Should().BeEquivalentTo(idsBA);
    }

    [Fact]
    public async Task GetNotamsForRouteAsync_SingleAirport_ShouldWork()
    {
        // Arrange
        SeedNotams(
            CreateNotamEntity("0000000000000001", "DFW", "KDFW"),
            CreateNotamEntity("0000000000000002", "DFW", "KDFW")
        );

        var request = new NotamQueryByRouteRequest
        {
            AirportIdentifiers = ["KDFW"]
        };

        // Act
        var result = await _service.GetNotamsForRouteAsync(request);

        // Assert
        result.TotalCount.Should().Be(2);
        result.QueryLocation.Should().Be("KDFW");
    }

    [Fact]
    public async Task GetNotamsForRouteAsync_ShouldHandleEmptyResultsForOneAirport()
    {
        // Arrange — only DFW has NOTAMs, AUS has none
        SeedNotams(
            CreateNotamEntity("0000000000000001", "DFW", "KDFW"),
            CreateNotamEntity("0000000000000002", "DFW", "KDFW")
        );

        var request = new NotamQueryByRouteRequest
        {
            AirportIdentifiers = ["KDFW", "KAUS"]
        };

        // Act
        var result = await _service.GetNotamsForRouteAsync(request);

        // Assert — only the DFW NOTAMs
        result.TotalCount.Should().Be(2);
        result.QueryLocation.Should().Be("KDFW -> KAUS");
    }

    [Fact]
    public async Task GetNotamsForRouteAsync_ShouldDeduplicateAcrossAirports()
    {
        // Arrange — same NOTAM is visible via both Location and IcaoLocation matches
        // A NOTAM with Location="DFW" and IcaoLocation="KDFW" appears when querying either
        SeedNotams(
            CreateNotamEntity("0000000000000001", "DFW", "KDFW")
        );

        var request = new NotamQueryByRouteRequest
        {
            AirportIdentifiers = ["DFW", "KDFW"]
        };

        // Act
        var result = await _service.GetNotamsForRouteAsync(request);

        // Assert — dedup should prevent the same NOTAM from appearing twice
        result.TotalCount.Should().Be(1);
    }

    #endregion

    #region Route Points Tests

    [Fact]
    public async Task GetNotamsForRouteAsync_RoutePoints_ShouldUseNamedWaypointInDescription()
    {
        // Arrange
        SeedNotams(
            CreateNotamEntity("0000000000000001", "DFW", "KDFW")
        );

        var request = new NotamQueryByRouteRequest
        {
            RoutePoints =
            [
                new RoutePointDto { AirportIdentifier = "KDFW" },
                new RoutePointDto { Name = "MAVER", Latitude = 32.5, Longitude = -97.0 }
            ]
        };

        // Act
        var result = await _service.GetNotamsForRouteAsync(request);

        // Assert — route description should include the named waypoint
        result.QueryLocation.Should().Contain("KDFW");
        result.QueryLocation.Should().Contain("MAVER");
    }

    [Fact]
    public async Task GetNotamsForRouteAsync_RoutePoints_ShouldThrowOnInvalidWaypoint()
    {
        // Arrange — waypoint without lat/lon
        var request = new NotamQueryByRouteRequest
        {
            RoutePoints =
            [
                new RoutePointDto { AirportIdentifier = "KDFW" },
                new RoutePointDto { Latitude = null, Longitude = null }
            ]
        };

        // Act
        var act = () => _service.GetNotamsForRouteAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*latitude and longitude*");
    }

    #endregion

    #region Helpers

    private void SeedNotams(params Notam[] notams)
    {
        foreach (var notam in notams)
        {
            if (!_dbContext.Notams.Any(n => n.NmsId == notam.NmsId))
            {
                _dbContext.Notams.Add(notam);
            }
        }
        _dbContext.SaveChanges();
    }

    private static Notam CreateNotamEntity(
        string nmsId,
        string location,
        string icaoLocation,
        string? text = null,
        DateTime? effectiveStart = null,
        DateTime? effectiveEnd = null)
    {
        var dto = new NotamDto
        {
            Type = "Feature",
            Id = nmsId,
            Properties = new NotamPropertiesDto
            {
                CoreNotamData = new CoreNotamDataDto
                {
                    Notam = new NotamDetailDto
                    {
                        Id = nmsId,
                        Number = "001",
                        Location = location,
                        IcaoLocation = icaoLocation,
                        Classification = "DOMESTIC",
                        Type = "N",
                        Text = text ?? $"Test NOTAM for {location}",
                        EffectiveStart = (effectiveStart ?? DateTime.UtcNow.AddHours(-1)).ToString("O"),
                        EffectiveEnd = effectiveEnd?.ToString("O")
                    }
                }
            }
        };

        return new Notam
        {
            NmsId = nmsId,
            Location = location,
            IcaoLocation = icaoLocation,
            Classification = "DOMESTIC",
            NotamType = "N",
            NotamNumber = "001",
            EffectiveStart = effectiveStart ?? DateTime.UtcNow.AddHours(-1),
            EffectiveEnd = effectiveEnd,
            Text = text ?? $"Test NOTAM for {location}",
            LastUpdated = DateTime.UtcNow,
            SyncedAt = DateTime.UtcNow,
            FeatureJson = JsonSerializer.Serialize(dto, JsonOptions)
        };
    }

    #endregion
}

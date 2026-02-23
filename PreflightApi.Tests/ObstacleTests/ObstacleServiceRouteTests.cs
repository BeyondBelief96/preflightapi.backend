using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NSubstitute;
using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos.Navlog;
using PreflightApi.Infrastructure.Services;
using Xunit;

namespace PreflightApi.Tests.ObstacleTests;

/// <summary>
/// Tests for ObstacleService route corridor logic — BuildRouteLineString and
/// GetObstacleOasNumbersForRouteAsync non-spatial edge cases.
/// Note: Spatial queries (IsWithinDistance) don't work with InMemory provider;
/// those are covered by integration tests with real PostGIS.
/// </summary>
public class ObstacleServiceRouteTests : IDisposable
{
    private readonly PreflightApiDbContext _dbContext;
    private readonly ObstacleService _service;

    public ObstacleServiceRouteTests()
    {
        var options = new DbContextOptionsBuilder<PreflightApiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PreflightApiDbContext(options);
        var logger = Substitute.For<ILogger<ObstacleService>>();

        _service = new ObstacleService(_dbContext, logger);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetObstacleOasNumbersForRouteAsync Edge Cases

    [Fact]
    public async Task GetObstacleOasNumbersForRouteAsync_ShouldReturnEmpty_WhenFewerThan2Waypoints()
    {
        // Arrange — only 1 waypoint
        var waypoints = new List<WaypointDto>
        {
            new() { Latitude = 32.9, Longitude = -97.0, Altitude = 3000, WaypointType = WaypointType.Airport }
        };

        // Act
        var result = await _service.GetObstacleOasNumbersForRouteAsync(waypoints);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetObstacleOasNumbersForRouteAsync_ShouldReturnEmpty_ForEmptyWaypoints()
    {
        // Arrange
        var waypoints = new List<WaypointDto>();

        // Act
        var result = await _service.GetObstacleOasNumbersForRouteAsync(waypoints);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region BuildRouteLineString Tests

    [Fact]
    public void BuildRouteLineString_ShouldReturnNull_WhenFewerThan2Waypoints()
    {
        // Arrange
        var waypoints = new List<WaypointDto>
        {
            new() { Latitude = 32.9, Longitude = -97.0, Altitude = 3000 }
        };

        // Act
        var result = _service.BuildRouteLineString(waypoints);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void BuildRouteLineString_ShouldCreateValidLineString_From2Waypoints()
    {
        // Arrange — DFW to AUS
        var waypoints = new List<WaypointDto>
        {
            new() { Latitude = 32.8968, Longitude = -97.0380, Altitude = 600 },
            new() { Latitude = 30.1945, Longitude = -97.6699, Altitude = 542 }
        };

        // Act
        var result = _service.BuildRouteLineString(waypoints);

        // Assert
        result.Should().NotBeNull();
        result!.GeometryType.Should().Be("LineString");
        result.NumPoints.Should().Be(2);
        result.SRID.Should().Be(4326);
    }

    [Fact]
    public void BuildRouteLineString_ShouldCreateValidLineString_FromMultipleWaypoints()
    {
        // Arrange — DFW → enroute waypoint → AUS
        var waypoints = new List<WaypointDto>
        {
            new() { Latitude = 32.8968, Longitude = -97.0380, Altitude = 600 },
            new() { Latitude = 31.5000, Longitude = -97.3000, Altitude = 5500 },
            new() { Latitude = 30.1945, Longitude = -97.6699, Altitude = 542 }
        };

        // Act
        var result = _service.BuildRouteLineString(waypoints);

        // Assert
        result.Should().NotBeNull();
        result!.NumPoints.Should().Be(3);
    }

    [Fact]
    public void BuildRouteLineString_ShouldPreserveLonLatOrder()
    {
        // Arrange — NTS uses (X=lon, Y=lat)
        var waypoints = new List<WaypointDto>
        {
            new() { Latitude = 32.8968, Longitude = -97.0380, Altitude = 600 },
            new() { Latitude = 30.1945, Longitude = -97.6699, Altitude = 542 }
        };

        // Act
        var result = _service.BuildRouteLineString(waypoints);

        // Assert — verify X=lon, Y=lat for the first coordinate
        result.Should().NotBeNull();
        var firstCoord = result!.Coordinates[0];
        firstCoord.X.Should().BeApproximately(-97.0380, 0.0001); // Longitude
        firstCoord.Y.Should().BeApproximately(32.8968, 0.0001);  // Latitude
    }

    [Fact]
    public void BuildRouteLineString_ShouldReturnNull_ForEmptyList()
    {
        // Arrange
        var waypoints = new List<WaypointDto>();

        // Act
        var result = _service.BuildRouteLineString(waypoints);

        // Assert
        result.Should().BeNull();
    }

    #endregion
}

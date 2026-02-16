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

public class NotamServiceTests : IDisposable
{
    private readonly PreflightApiDbContext _dbContext;
    private readonly IOptions<NmsSettings> _settings;
    private readonly ILogger<NotamService> _logger;
    private readonly NotamService _service;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public NotamServiceTests()
    {
        var options = new DbContextOptionsBuilder<PreflightApiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PreflightApiDbContext(options);
        _settings = Options.Create(new NmsSettings
        {
            CacheDurationMinutes = 5,
            DefaultRouteCorridorRadiusNm = 25
        });
        _logger = Substitute.For<ILogger<NotamService>>();

        _service = new NotamService(_dbContext, _settings, _logger);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region Airport Query Tests

    [Fact]
    public async Task GetNotamsForAirportAsync_ShouldReturnNotams_WhenDataExists()
    {
        // Arrange
        SeedNotams(
            CreateNotamEntity("0000000000000001", "DFW", "KDFW"),
            CreateNotamEntity("0000000000000002", "DFW", "KDFW")
        );

        // Act
        var result = await _service.GetNotamsForAirportAsync("KDFW");

        // Assert
        result.Should().NotBeNull();
        result.Notams.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.QueryLocation.Should().Be("KDFW");
    }

    [Fact]
    public async Task GetNotamsForAirportAsync_ShouldMatchByLocation()
    {
        // Arrange
        SeedNotams(
            CreateNotamEntity("0000000000000001", "DFW", "KDFW")
        );

        // Act
        var result = await _service.GetNotamsForAirportAsync("DFW");

        // Assert
        result.Notams.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetNotamsForAirportAsync_ShouldMatchByIcaoLocation()
    {
        // Arrange
        SeedNotams(
            CreateNotamEntity("0000000000000001", "DFW", "KDFW")
        );

        // Act
        var result = await _service.GetNotamsForAirportAsync("KDFW");

        // Assert
        result.Notams.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetNotamsForAirportAsync_ShouldNormalizeIdentifier()
    {
        // Arrange
        SeedNotams(
            CreateNotamEntity("0000000000000001", "DFW", "KDFW")
        );

        // Act
        var result = await _service.GetNotamsForAirportAsync("  kdfw  ");

        // Assert
        result.QueryLocation.Should().Be("KDFW");
        result.Notams.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetNotamsForAirportAsync_ShouldExcludeCancelledNotams()
    {
        // Arrange — cancellation date in the past means NOTAM was manually terminated
        SeedNotams(
            CreateNotamEntity("0000000000000001", "DFW", "KDFW"),
            CreateNotamEntity("0000000000000002", "DFW", "KDFW",
                cancelationDate: DateTime.UtcNow.AddHours(-1)) // cancelled an hour ago
        );

        // Act
        var result = await _service.GetNotamsForAirportAsync("KDFW");

        // Assert — only the active NOTAM is returned
        result.Notams.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetNotamsForAirportAsync_ShouldIncludeNotamsWithFutureCancelationDate()
    {
        // Arrange — cancellation date in the future (edge case: scheduled but not yet effective)
        SeedNotams(
            CreateNotamEntity("0000000000000001", "DFW", "KDFW"),
            CreateNotamEntity("0000000000000002", "DFW", "KDFW",
                cancelationDate: DateTime.UtcNow.AddHours(1)) // not yet cancelled
        );

        // Act
        var result = await _service.GetNotamsForAirportAsync("KDFW");

        // Assert — both returned since cancellation hasn't taken effect
        result.Notams.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetNotamsForAirportAsync_ShouldExcludeExpiredNotams()
    {
        // Arrange
        SeedNotams(
            CreateNotamEntity("0000000000000001", "DFW", "KDFW",
                effectiveEnd: DateTime.UtcNow.AddHours(1)), // Still active
            CreateNotamEntity("0000000000000002", "DFW", "KDFW",
                effectiveEnd: DateTime.UtcNow.AddHours(-1)) // Expired
        );

        // Act
        var result = await _service.GetNotamsForAirportAsync("KDFW");

        // Assert
        result.Notams.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetNotamsForAirportAsync_ShouldIncludeNotams_WithNullEffectiveEnd()
    {
        // Arrange — permanent NOTAMs have no end date
        SeedNotams(
            CreateNotamEntity("0000000000000001", "DFW", "KDFW", effectiveEnd: null)
        );

        // Act
        var result = await _service.GetNotamsForAirportAsync("KDFW");

        // Assert
        result.Notams.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetNotamsForAirportAsync_ShouldThrowArgumentException_WhenIdentifierIsEmpty()
    {
        // Act
        Func<Task> act = async () => await _service.GetNotamsForAirportAsync("");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*cannot be null or empty*");
    }

    [Fact]
    public async Task GetNotamsForAirportAsync_ShouldReturnEmpty_WhenNoNotamsExist()
    {
        // Act
        var result = await _service.GetNotamsForAirportAsync("KDFW");

        // Assert
        result.Notams.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetNotamsForAirportAsync_ShouldFilterByClassification()
    {
        // Arrange
        SeedNotams(
            CreateNotamEntity("0000000000000001", "DFW", "KDFW", classification: "DOMESTIC"),
            CreateNotamEntity("0000000000000002", "DFW", "KDFW", classification: "FDC")
        );

        var filters = new NotamFilterDto { Classification = "DOMESTIC" };

        // Act
        var result = await _service.GetNotamsForAirportAsync("KDFW", filters);

        // Assert
        result.Notams.Should().HaveCount(1);
    }

    #endregion

    #region Radius Query Tests

    [Fact]
    public async Task GetNotamsByRadiusAsync_ShouldThrowArgumentOutOfRangeException_WhenRadiusTooLarge()
    {
        // Act
        Func<Task> act = async () => await _service.GetNotamsByRadiusAsync(32.897, -97.038, 150.0);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*cannot exceed 100*");
    }

    [Fact]
    public async Task GetNotamsByRadiusAsync_ShouldThrowArgumentOutOfRangeException_WhenRadiusIsZero()
    {
        // Act
        Func<Task> act = async () => await _service.GetNotamsByRadiusAsync(32.897, -97.038, 0);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*must be greater than 0*");
    }

    [Fact]
    public async Task GetNotamsByRadiusAsync_ShouldFormatQueryLocation()
    {
        // Act (InMemory won't have spatial data, but we can verify format)
        var result = await _service.GetNotamsByRadiusAsync(32.897, -97.038, 25.0);

        // Assert
        result.QueryLocation.Should().Contain("32.8970");
        result.QueryLocation.Should().Contain("-97.0380");
        result.QueryLocation.Should().Contain("25");
    }

    #endregion

    #region Route Tests

    [Fact]
    public async Task GetNotamsForRouteAsync_ShouldAggregateNotams_FromMultipleAirports()
    {
        // Arrange
        SeedNotams(
            CreateNotamEntity("0000000000000001", "DFW", "KDFW"),
            CreateNotamEntity("0000000000000002", "DFW", "KDFW"),
            CreateNotamEntity("0000000000000003", "ORD", "KORD"),
            CreateNotamEntity("0000000000000004", "ORD", "KORD")
        );

        var request = new NotamQueryByRouteRequest
        {
            AirportIdentifiers = ["KDFW", "KORD"]
        };

        // Act
        var result = await _service.GetNotamsForRouteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Notams.Should().HaveCount(4);
        result.QueryLocation.Should().Be("KDFW -> KORD");
    }

    [Fact]
    public async Task GetNotamsForRouteAsync_ShouldDeduplicateNotams_ByNotamId()
    {
        // Arrange — NOTAM visible to both airports (matches both Location fields)
        // The NOTAM has Location=DFW and IcaoLocation=KORD, so it matches queries for both airports
        SeedNotams(
            CreateNotamEntity("0000000000000001", "DFW", "KORD"), // Matches both KDFW (no) and KORD (yes by IcaoLocation), and DFW (yes by Location)
            CreateNotamEntity("0000000000000002", "DFW", "KDFW"),
            CreateNotamEntity("0000000000000003", "ORD", "KORD")
        );

        var request = new NotamQueryByRouteRequest
        {
            AirportIdentifiers = ["DFW", "KORD"]
        };

        // Act
        var result = await _service.GetNotamsForRouteAsync(request);

        // Assert — NOTAM 0000000000000001 matches both DFW (by Location) and KORD (by IcaoLocation)
        // but should only appear once due to deduplication
        result.Notams.Should().HaveCount(3); // 3 unique, not 4
    }

    [Fact]
    public async Task GetNotamsForRouteAsync_ShouldThrowArgumentException_WhenNoAirportsOrRoutePoints()
    {
        // Arrange
        var request = new NotamQueryByRouteRequest
        {
            AirportIdentifiers = [],
            RoutePoints = []
        };

        // Act
        Func<Task> act = async () => await _service.GetNotamsForRouteAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*At least one airport*");
    }

    [Fact]
    public async Task GetNotamsForRouteAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Act
        Func<Task> act = async () => await _service.GetNotamsForRouteAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region RoutePoints Tests

    [Fact]
    public async Task GetNotamsForRouteAsync_RoutePoints_ShouldReturnNotams_ForAirportsOnly()
    {
        // Arrange
        SeedNotams(
            CreateNotamEntity("0000000000000001", "DFW", "KDFW"),
            CreateNotamEntity("0000000000000002", "DFW", "KDFW"),
            CreateNotamEntity("0000000000000003", "AUS", "KAUS")
        );

        var request = new NotamQueryByRouteRequest
        {
            RoutePoints =
            [
                new RoutePointDto { AirportIdentifier = "KDFW" },
                new RoutePointDto { AirportIdentifier = "KAUS" }
            ]
        };

        // Act
        var result = await _service.GetNotamsForRouteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Notams.Should().HaveCount(3);
        result.QueryLocation.Should().Be("KDFW -> KAUS");
    }

    [Fact]
    public async Task GetNotamsForRouteAsync_RoutePoints_ShouldFormatUnnamedWaypointCoordinates()
    {
        // Arrange — no matching NOTAMs needed for this test
        var request = new NotamQueryByRouteRequest
        {
            RoutePoints =
            [
                new RoutePointDto { Latitude = 30.1234, Longitude = -97.5678 }
            ],
            CorridorRadiusNm = 25
        };

        // Act
        var result = await _service.GetNotamsForRouteAsync(request);

        // Assert
        result.QueryLocation.Should().Be("30.1234N, 97.5678W");
    }

    [Fact]
    public async Task GetNotamsForRouteAsync_RoutePoints_ShouldFormatNegativeLatitude()
    {
        // Arrange
        var request = new NotamQueryByRouteRequest
        {
            RoutePoints =
            [
                new RoutePointDto { Latitude = -33.9465, Longitude = 18.6017 }
            ],
            CorridorRadiusNm = 25
        };

        // Act
        var result = await _service.GetNotamsForRouteAsync(request);

        // Assert
        result.QueryLocation.Should().Be("33.9465S, 18.6017E");
    }

    [Fact]
    public async Task GetNotamsForRouteAsync_RoutePoints_ShouldThrowArgumentException_WhenWaypointMissingLatitude()
    {
        // Arrange
        var request = new NotamQueryByRouteRequest
        {
            RoutePoints =
            [
                new RoutePointDto { Longitude = -97.5678 }
            ]
        };

        // Act
        Func<Task> act = async () => await _service.GetNotamsForRouteAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Waypoints require both latitude and longitude*");
    }

    [Fact]
    public async Task GetNotamsForRouteAsync_RoutePoints_ShouldThrowArgumentException_WhenLatitudeOutOfRange()
    {
        // Arrange
        var request = new NotamQueryByRouteRequest
        {
            RoutePoints =
            [
                new RoutePointDto { Latitude = 91.0, Longitude = -97.5678 }
            ]
        };

        // Act
        Func<Task> act = async () => await _service.GetNotamsForRouteAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Latitude must be between -90 and 90*");
    }

    [Fact]
    public async Task GetNotamsForRouteAsync_RoutePoints_ShouldThrowArgumentException_WhenLongitudeOutOfRange()
    {
        // Arrange
        var request = new NotamQueryByRouteRequest
        {
            RoutePoints =
            [
                new RoutePointDto { Latitude = 30.0, Longitude = -181.0 }
            ]
        };

        // Act
        Func<Task> act = async () => await _service.GetNotamsForRouteAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Longitude must be between -180 and 180*");
    }

    [Fact]
    public async Task GetNotamsForRouteAsync_RoutePoints_ShouldThrowArgumentException_WhenRadiusTooLarge()
    {
        // Arrange
        var request = new NotamQueryByRouteRequest
        {
            RoutePoints =
            [
                new RoutePointDto { Latitude = 30.0, Longitude = -97.0, RadiusNm = 150 }
            ]
        };

        // Act
        Func<Task> act = async () => await _service.GetNotamsForRouteAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Radius cannot exceed 100*");
    }

    [Fact]
    public async Task GetNotamsForRouteAsync_RoutePoints_ShouldThrowArgumentException_WhenRadiusIsZero()
    {
        // Arrange
        var request = new NotamQueryByRouteRequest
        {
            RoutePoints =
            [
                new RoutePointDto { Latitude = 30.0, Longitude = -97.0, RadiusNm = 0 }
            ]
        };

        // Act
        Func<Task> act = async () => await _service.GetNotamsForRouteAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Radius must be greater than 0*");
    }

    [Fact]
    public async Task GetNotamsForRouteAsync_RoutePoints_TakesPrecedence_OverAirportIdentifiers()
    {
        // Arrange
        SeedNotams(
            CreateNotamEntity("0000000000000001", "AUS", "KAUS"),
            CreateNotamEntity("0000000000000002", "DFW", "KDFW")
        );

        var request = new NotamQueryByRouteRequest
        {
            AirportIdentifiers = ["KDFW"], // Should be ignored
            RoutePoints =
            [
                new RoutePointDto { AirportIdentifier = "KAUS" }
            ]
        };

        // Act
        var result = await _service.GetNotamsForRouteAsync(request);

        // Assert
        result.QueryLocation.Should().Be("KAUS");
        result.Notams.Should().HaveCount(1);
        result.Notams[0].Properties!.CoreNotamData!.Notam!.Location.Should().Be("AUS");
    }

    [Fact]
    public async Task GetNotamsForRouteAsync_RoutePoints_PreservesOrderInRouteDescription()
    {
        // Arrange
        SeedNotams(
            CreateNotamEntity("0000000000000001", "DFW", "KDFW"),
            CreateNotamEntity("0000000000000004", "AUS", "KAUS")
        );

        var request = new NotamQueryByRouteRequest
        {
            RoutePoints =
            [
                new RoutePointDto { AirportIdentifier = "KDFW" },
                new RoutePointDto { Name = "Lake Travis", Latitude = 30.4082, Longitude = -97.8538 },
                new RoutePointDto { Latitude = 30.1, Longitude = -97.6 },
                new RoutePointDto { AirportIdentifier = "KAUS" }
            ],
            CorridorRadiusNm = 25
        };

        // Act
        var result = await _service.GetNotamsForRouteAsync(request);

        // Assert
        result.QueryLocation.Should().Be("KDFW -> Lake Travis -> 30.1000N, 97.6000W -> KAUS");
    }

    #endregion

    #region NMS ID Tests

    [Fact]
    public async Task GetNotamByNmsIdAsync_ShouldReturnNotam_WhenFound()
    {
        // Arrange
        SeedNotams(
            CreateNotamEntity("1234567890123456", "DFW", "KDFW")
        );

        // Act
        var result = await _service.GetNotamByNmsIdAsync("1234567890123456");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("1234567890123456");
    }

    [Fact]
    public async Task GetNotamByNmsIdAsync_ShouldReturnNull_WhenNotFound()
    {
        // Act
        var result = await _service.GetNotamByNmsIdAsync("9999999999999999");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetNotamByNmsIdAsync_ShouldThrowArgumentException_WhenIdIsEmpty()
    {
        // Act
        Func<Task> act = async () => await _service.GetNotamByNmsIdAsync("");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*cannot be null or empty*");
    }

    [Fact]
    public async Task GetNotamByNmsIdAsync_ShouldReturnCanceledNotams()
    {
        // GetNotamByNmsIdAsync does not apply active filters — returns any NOTAM by ID
        SeedNotams(
            CreateNotamEntity("1234567890123456", "DFW", "KDFW", notamType: "C")
        );

        // Act
        var result = await _service.GetNotamByNmsIdAsync("1234567890123456");

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region Filter Tests

    [Fact]
    public async Task GetNotamsForAirportAsync_ShouldFilterByClassificationInRoute()
    {
        // Arrange
        SeedNotams(
            CreateNotamEntity("0000000000000001", "DFW", "KDFW", classification: "DOMESTIC"),
            CreateNotamEntity("0000000000000002", "DFW", "KDFW", classification: "FDC"),
            CreateNotamEntity("0000000000000003", "DFW", "KDFW", classification: "INTERNATIONAL")
        );

        var filters = new NotamFilterDto { Classification = "FDC" };

        // Act
        var result = await _service.GetNotamsForAirportAsync("KDFW", filters);

        // Assert
        result.Notams.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetNotamsForRouteAsync_ShouldPassFilters()
    {
        // Arrange
        SeedNotams(
            CreateNotamEntity("0000000000000001", "DFW", "KDFW", classification: "DOMESTIC"),
            CreateNotamEntity("0000000000000002", "DFW", "KDFW", classification: "FDC"),
            CreateNotamEntity("0000000000000003", "AUS", "KAUS", classification: "DOMESTIC"),
            CreateNotamEntity("0000000000000004", "AUS", "KAUS", classification: "FDC")
        );

        var request = new NotamQueryByRouteRequest
        {
            AirportIdentifiers = ["KDFW", "KAUS"],
            Filters = new NotamFilterDto { Classification = "DOMESTIC" }
        };

        // Act
        var result = await _service.GetNotamsForRouteAsync(request);

        // Assert
        result.Notams.Should().HaveCount(2);
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task SearchNotamsAsync_ShouldReturnPaginatedNotams_FilteredByClassification()
    {
        // Arrange
        SeedNotams(
            CreateNotamEntity("0000000000000001", "DFW", "KDFW", classification: "DOMESTIC"),
            CreateNotamEntity("0000000000000002", "AUS", "KAUS", classification: "FDC"),
            CreateNotamEntity("0000000000000003", "ORD", "KORD", classification: "DOMESTIC")
        );

        var filters = new NotamFilterDto { Classification = "DOMESTIC" };

        // Act
        var result = await _service.SearchNotamsAsync(filters);

        // Assert
        result.Data.Should().HaveCount(2);
        result.Pagination.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task SearchNotamsAsync_ShouldExcludeCancelledAndExpiredNotams()
    {
        // Arrange
        SeedNotams(
            CreateNotamEntity("0000000000000001", "DFW", "KDFW", classification: "DOMESTIC"),
            CreateNotamEntity("0000000000000002", "AUS", "KAUS", classification: "DOMESTIC",
                cancelationDate: DateTime.UtcNow.AddHours(-1)), // manually cancelled — excluded
            CreateNotamEntity("0000000000000003", "ORD", "KORD", classification: "DOMESTIC",
                effectiveEnd: DateTime.UtcNow.AddHours(-1)) // naturally expired — excluded
        );

        var filters = new NotamFilterDto { Classification = "DOMESTIC" };

        // Act
        var result = await _service.SearchNotamsAsync(filters);

        // Assert — only the active NOTAM is returned
        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchNotamsAsync_ShouldRespectLimit()
    {
        // Arrange
        SeedNotams(
            CreateNotamEntity("0000000000000001", "DFW", "KDFW", classification: "DOMESTIC"),
            CreateNotamEntity("0000000000000002", "AUS", "KAUS", classification: "DOMESTIC"),
            CreateNotamEntity("0000000000000003", "ORD", "KORD", classification: "DOMESTIC")
        );

        var filters = new NotamFilterDto { Classification = "DOMESTIC" };

        // Act
        var result = await _service.SearchNotamsAsync(filters, cursor: null, limit: 2);

        // Assert
        result.Data.Should().HaveCount(2);
        result.Pagination.HasMore.Should().BeTrue();
        result.Pagination.NextCursor.Should().NotBeNullOrEmpty();
        result.Pagination.Limit.Should().Be(2);
    }

    [Fact]
    public async Task SearchNotamsAsync_ShouldPaginateWithCursor()
    {
        // Arrange
        SeedNotams(
            CreateNotamEntity("0000000000000001", "DFW", "KDFW", classification: "DOMESTIC"),
            CreateNotamEntity("0000000000000002", "AUS", "KAUS", classification: "DOMESTIC"),
            CreateNotamEntity("0000000000000003", "ORD", "KORD", classification: "DOMESTIC")
        );

        var filters = new NotamFilterDto { Classification = "DOMESTIC" };

        // Act — first page
        var page1 = await _service.SearchNotamsAsync(filters, cursor: null, limit: 2);

        // Act — second page using cursor from first
        var page2 = await _service.SearchNotamsAsync(filters, cursor: page1.Pagination.NextCursor, limit: 2);

        // Assert
        page1.Data.Should().HaveCount(2);
        page1.Pagination.HasMore.Should().BeTrue();

        page2.Data.Should().HaveCount(1);
        page2.Pagination.HasMore.Should().BeFalse();
        page2.Pagination.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task SearchNotamsAsync_ShouldThrowArgumentException_WhenNoFiltersProvided()
    {
        // Act
        Func<Task> act = async () => await _service.SearchNotamsAsync(new NotamFilterDto());

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*At least one filter*");
    }

    [Fact]
    public async Task SearchNotamsAsync_ShouldThrowArgumentException_WhenFiltersIsNull()
    {
        // Act
        Func<Task> act = async () => await _service.SearchNotamsAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*At least one filter*");
    }

    #endregion

    #region Number Search Tests

    [Fact]
    public async Task GetNotamsByNumberAsync_ShouldReturnNotam_ByBareNumber()
    {
        // Arrange
        SeedNotams(
            CreateNotamEntity("0000000000000001", "DFW", "KDFW", notamNumber: "420", notamYear: "2025", accountId: "DFW"),
            CreateNotamEntity("0000000000000002", "AUS", "KAUS", notamNumber: "999", notamYear: "2025", accountId: "AUS")
        );

        // Act
        var results = await _service.GetNotamsByNumberAsync("420");

        // Assert
        results.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetNotamsByNumberAsync_ShouldReturnNotam_WithYear()
    {
        // Arrange
        SeedNotams(
            CreateNotamEntity("0000000000000001", "DFW", "KDFW", notamNumber: "420", notamYear: "2025", accountId: "DFW"),
            CreateNotamEntity("0000000000000002", "DFW", "KDFW", notamNumber: "420", notamYear: "2024", accountId: "DFW")
        );

        // Act
        var results = await _service.GetNotamsByNumberAsync("420/2025");

        // Assert
        results.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetNotamsByNumberAsync_ShouldReturnNotam_WithAccountId()
    {
        // Arrange
        SeedNotams(
            CreateNotamEntity("0000000000000001", "BNA", "KBNA", notamNumber: "420", notamYear: "2025", accountId: "BNA"),
            CreateNotamEntity("0000000000000002", "DFW", "KDFW", notamNumber: "420", notamYear: "2025", accountId: "DFW")
        );

        // Act
        var results = await _service.GetNotamsByNumberAsync("BNA 420");

        // Assert
        results.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetNotamsByNumberAsync_ShouldReturnMultiple_ForAmbiguousBareNumber()
    {
        // Arrange
        SeedNotams(
            CreateNotamEntity("0000000000000001", "BNA", "KBNA", notamNumber: "420", notamYear: "2025", accountId: "BNA"),
            CreateNotamEntity("0000000000000002", "DFW", "KDFW", notamNumber: "420", notamYear: "2025", accountId: "DFW")
        );

        // Act
        var results = await _service.GetNotamsByNumberAsync("420");

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetNotamsByNumberAsync_ShouldReturnEmpty_WhenNoMatch()
    {
        // Arrange
        SeedNotams(
            CreateNotamEntity("0000000000000001", "DFW", "KDFW", notamNumber: "420", notamYear: "2025", accountId: "DFW")
        );

        // Act
        var results = await _service.GetNotamsByNumberAsync("999");

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNotamsByNumberAsync_ShouldThrowArgumentException_ForInvalidInput()
    {
        // Act
        Func<Task> act = async () => await _service.GetNotamsByNumberAsync("");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*cannot be null or empty*");
    }

    [Fact]
    public async Task GetNotamsByNumberAsync_ShouldIncludeCancelledNotams()
    {
        // Number search does not apply active filters — returns cancelled NOTAMs too
        SeedNotams(
            CreateNotamEntity("0000000000000001", "DFW", "KDFW",
                notamNumber: "420", notamYear: "2025", accountId: "DFW",
                cancelationDate: DateTime.UtcNow.AddHours(-1))
        );

        // Act
        var results = await _service.GetNotamsByNumberAsync("420");

        // Assert
        results.Should().HaveCount(1);
    }

    #endregion

    #region Helpers

    private void SeedNotams(params Notam[] notams)
    {
        foreach (var notam in notams)
        {
            // Avoid duplicate key errors in seed — skip if already exists
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
        string? classification = null,
        string notamType = "N",
        DateTime? effectiveStart = null,
        DateTime? effectiveEnd = null,
        DateTime? cancelationDate = null,
        string? notamNumber = null,
        string? notamYear = null,
        string? accountId = null,
        string? airportName = null)
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
                        Number = notamNumber ?? "001",
                        Location = location,
                        IcaoLocation = icaoLocation,
                        Classification = classification ?? "DOMESTIC",
                        Type = notamType,
                        Text = $"Test NOTAM for {location}",
                        EffectiveStart = (effectiveStart ?? DateTime.UtcNow.AddHours(-1)).ToString("O"),
                        EffectiveEnd = effectiveEnd?.ToString("O"),
                        CancelationDate = cancelationDate?.ToString("O"),
                        AccountId = accountId,
                        Year = notamYear,
                        AirportName = airportName
                    }
                }
            }
        };

        return new Notam
        {
            NmsId = nmsId,
            Location = location,
            IcaoLocation = icaoLocation,
            Classification = classification ?? "DOMESTIC",
            NotamType = notamType,
            NotamNumber = notamNumber ?? "001",
            NotamYear = notamYear,
            AccountId = accountId,
            AirportName = airportName,
            EffectiveStart = effectiveStart ?? DateTime.UtcNow.AddHours(-1),
            EffectiveEnd = effectiveEnd,
            CancelationDate = cancelationDate,
            Text = $"Test NOTAM for {location}",
            LastUpdated = DateTime.UtcNow,
            SyncedAt = DateTime.UtcNow,
            FeatureJson = JsonSerializer.Serialize(dto, JsonOptions)
        };
    }

    #endregion
}

using FluentAssertions;
using Microsoft.Extensions.Logging;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NSubstitute;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Services;
using Xunit;

namespace PreflightApi.Tests.IntegrationTests;

public class AirportServiceSearchNearbyTests : PostgreSqlTestBase
{
    private readonly GeometryFactory _geometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    // DFW coordinates
    private const decimal DfwLat = 32.8968m;
    private const decimal DfwLon = -97.0380m;

    protected override async Task SeedDatabaseAsync()
    {
        // DFW — at the search center
        DbContext.Airports.Add(CreateAirport("50000", "KDFW", "DFW", "DFW Intl", DfwLat, DfwLon));

        // DAL — ~10 NM from DFW
        DbContext.Airports.Add(CreateAirport("50001", "KDAL", "DAL", "Dallas Love Field", 32.8471m, -96.8518m));

        // AFW — ~15 NM from DFW
        DbContext.Airports.Add(CreateAirport("50002", "KAFW", "AFW", "Fort Worth Alliance", 32.9876m, -97.3188m));

        // AUS — ~160 NM from DFW (outside any reasonable search radius)
        DbContext.Airports.Add(CreateAirport("50003", "KAUS", "AUS", "Austin-Bergstrom", 30.1945m, -97.6699m));

        // Airport with no coordinates
        DbContext.Airports.Add(new Airport { SiteNo = "50004", ArptId = "NOCOORD", ArptName = "No Coordinates" });

        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task SearchNearby_ReturnsAirportsWithinRadius()
    {
        var service = CreateService();

        // 20 NM from DFW includes DFW (~0), DAL (~10), and AFW (~15)
        var result = await service.SearchNearby(DfwLat, DfwLon, 20, null, 100);

        result.Data.Should().HaveCount(3);
        result.Data.Select(a => a.ArptId).Should().Contain("DFW").And.Contain("DAL").And.Contain("AFW");
    }

    [Fact]
    public async Task SearchNearby_ExcludesAirportsOutsideRadius()
    {
        var service = CreateService();

        var result = await service.SearchNearby(DfwLat, DfwLon, 20, null, 100);

        result.Data.Select(a => a.ArptId).Should().NotContain("AUS");
    }

    [Fact]
    public async Task SearchNearby_ExcludesAirportsWithNullLocation()
    {
        var service = CreateService();

        var result = await service.SearchNearby(DfwLat, DfwLon, 1000, null, 100);

        result.Data.Select(a => a.ArptId).Should().NotContain("NOCOORD");
    }

    [Fact]
    public async Task SearchNearby_LargerRadius_IncludesMoreAirports()
    {
        var service = CreateService();

        // 12 NM includes DFW and DAL but not AFW (~15 NM)
        var smallResult = await service.SearchNearby(DfwLat, DfwLon, 12, null, 100);
        smallResult.Data.Should().HaveCount(2);

        // 20 NM also includes AFW
        var largeResult = await service.SearchNearby(DfwLat, DfwLon, 20, null, 100);
        largeResult.Data.Should().HaveCount(3);
        largeResult.Data.Select(a => a.ArptId).Should().Contain("AFW");
    }

    [Fact]
    public async Task SearchNearby_SmallRadius_ReturnsOnlyClosest()
    {
        var service = CreateService();

        var result = await service.SearchNearby(DfwLat, DfwLon, 1, null, 100);

        result.Data.Should().HaveCount(1);
        result.Data.First().ArptId.Should().Be("DFW");
    }

    [Fact]
    public async Task SearchNearby_RespectsLimit()
    {
        var service = CreateService();

        var result = await service.SearchNearby(DfwLat, DfwLon, 20, null, 2);

        result.Data.Should().HaveCount(2);
        result.Pagination.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task SearchNearby_PaginationWorks()
    {
        var service = CreateService();

        var page1 = await service.SearchNearby(DfwLat, DfwLon, 20, null, 2);
        page1.Data.Should().HaveCount(2);
        page1.Pagination.NextCursor.Should().NotBeNullOrEmpty();

        var page2 = await service.SearchNearby(DfwLat, DfwLon, 20, page1.Pagination.NextCursor, 2);
        page2.Data.Should().HaveCount(1);
        page2.Pagination.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task SearchNearby_NoResults_ReturnsEmpty()
    {
        var service = CreateService();

        // Search in the middle of the ocean
        var result = await service.SearchNearby(0m, 0m, 5, null, 100);

        result.Data.Should().BeEmpty();
        result.Pagination.HasMore.Should().BeFalse();
    }

    private AirportService CreateService()
    {
        return new AirportService(DbContext, Substitute.For<ILogger<AirportService>>());
    }

    private Airport CreateAirport(string siteNo, string icaoId, string arptId, string name, decimal lat, decimal lon)
    {
        return new Airport
        {
            SiteNo = siteNo,
            IcaoId = icaoId,
            ArptId = arptId,
            ArptName = name,
            LatDecimal = lat,
            LongDecimal = lon,
            Location = _geometryFactory.CreatePoint(new Coordinate((double)lon, (double)lat))
        };
    }
}

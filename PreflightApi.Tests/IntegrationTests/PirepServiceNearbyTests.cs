using FluentAssertions;
using Microsoft.Extensions.Logging;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NSubstitute;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Services.WeatherServices;
using Xunit;

namespace PreflightApi.Tests.IntegrationTests;

[Collection("Integration")]
public class PirepServiceNearbyTests : PostgreSqlTestBase
{
    private readonly GeometryFactory _geometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    // DFW coordinates
    private const double DfwLat = 32.8968;
    private const double DfwLon = -97.0380;

    protected override async Task SeedDatabaseAsync()
    {
        // PIREP at DFW (0 NM from search center)
        DbContext.Pireps.Add(CreatePirep(1, 32.8968f, -97.0380f, "UA /OV DFW"));

        // PIREP ~10 NM from DFW (near DAL)
        DbContext.Pireps.Add(CreatePirep(2, 32.8471f, -96.8518f, "UA /OV DAL"));

        // PIREP ~15 NM from DFW (near AFW)
        DbContext.Pireps.Add(CreatePirep(3, 32.9876f, -97.3188f, "UA /OV AFW"));

        // PIREP ~160 NM from DFW (near AUS — outside any reasonable search radius)
        DbContext.Pireps.Add(CreatePirep(4, 30.1945f, -97.6699f, "UA /OV AUS"));

        // PIREP with no coordinates
        DbContext.Pireps.Add(new Pirep { Id = 5, RawText = "UA /OV UNKNOWN", Latitude = null, Longitude = null });

        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task SearchNearby_ReturnsPirepsWithinRadius()
    {
        var service = CreateService();

        // 20 NM from DFW includes PIREPs at DFW (~0), DAL (~10), and AFW (~15)
        var result = await service.SearchNearby(DfwLat, DfwLon, 20, null, 100, CancellationToken.None);

        result.Data.Should().HaveCount(3);
        result.Data.Select(p => p.RawText).Should()
            .Contain("UA /OV DFW")
            .And.Contain("UA /OV DAL")
            .And.Contain("UA /OV AFW");
    }

    [Fact]
    public async Task SearchNearby_ExcludesPirepsOutsideRadius()
    {
        var service = CreateService();

        var result = await service.SearchNearby(DfwLat, DfwLon, 20, null, 100, CancellationToken.None);

        result.Data.Select(p => p.RawText).Should().NotContain("UA /OV AUS");
    }

    [Fact]
    public async Task SearchNearby_ExcludesPirepsWithNullLocation()
    {
        var service = CreateService();

        var result = await service.SearchNearby(DfwLat, DfwLon, 1000, null, 100, CancellationToken.None);

        result.Data.Select(p => p.RawText).Should().NotContain("UA /OV UNKNOWN");
    }

    [Fact]
    public async Task SearchNearby_SmallRadius_ReturnsOnlyClosest()
    {
        var service = CreateService();

        var result = await service.SearchNearby(DfwLat, DfwLon, 1, null, 100, CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Data.First().RawText.Should().Be("UA /OV DFW");
    }

    [Fact]
    public async Task SearchNearby_RespectsLimit()
    {
        var service = CreateService();

        var result = await service.SearchNearby(DfwLat, DfwLon, 20, null, 2, CancellationToken.None);

        result.Data.Should().HaveCount(2);
        result.Pagination.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task SearchNearby_PaginationWorks()
    {
        var service = CreateService();

        var page1 = await service.SearchNearby(DfwLat, DfwLon, 20, null, 2, CancellationToken.None);
        page1.Data.Should().HaveCount(2);
        page1.Pagination.NextCursor.Should().NotBeNullOrEmpty();

        var page2 = await service.SearchNearby(DfwLat, DfwLon, 20, page1.Pagination.NextCursor, 2, CancellationToken.None);
        page2.Data.Should().HaveCount(1);
        page2.Pagination.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task SearchNearby_NoResults_ReturnsEmpty()
    {
        var service = CreateService();

        // Search in the middle of the ocean
        var result = await service.SearchNearby(0, 0, 5, null, 100, CancellationToken.None);

        result.Data.Should().BeEmpty();
        result.Pagination.HasMore.Should().BeFalse();
    }

    private PirepService CreateService()
    {
        return new PirepService(DbContext, Substitute.For<ILogger<PirepService>>());
    }

    private Pirep CreatePirep(int id, float lat, float lon, string rawText)
    {
        return new Pirep
        {
            Id = id,
            Latitude = lat,
            Longitude = lon,
            RawText = rawText,
            ReportType = "PIREP",
            Location = _geometryFactory.CreatePoint(new Coordinate(lon, lat))
        };
    }
}

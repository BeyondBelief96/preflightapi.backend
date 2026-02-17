using FluentAssertions;
using Microsoft.Extensions.Logging;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NSubstitute;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Enums;
using PreflightApi.Domain.ValueObjects.GAirmets;
using PreflightApi.Infrastructure.Services.WeatherServices;
using Xunit;

namespace PreflightApi.Tests.IntegrationTests;

[Collection("Integration")]
public class GAirmetServiceSpatialTests : PostgreSqlTestBase
{
    private readonly GeometryFactory _geometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    // DFW area center
    private const decimal DfwLat = 32.8968m;
    private const decimal DfwLon = -97.0380m;

    protected override async Task SeedDatabaseAsync()
    {
        var now = DateTime.UtcNow;

        // G-AIRMET 1: Covers DFW area (large polygon around north Texas)
        DbContext.GAirmets.Add(CreateGAirmet(1, "SIERRA", "IFR", now,
            new[] { (-99.0, 31.0), (-95.0, 31.0), (-95.0, 35.0), (-99.0, 35.0) }));

        // G-AIRMET 2: Covers Houston area only (well south of DFW)
        DbContext.GAirmets.Add(CreateGAirmet(2, "ZULU", "ICE", now,
            new[] { (-96.0, 29.0), (-94.0, 29.0), (-94.0, 30.5), (-96.0, 30.5) }));

        // G-AIRMET 3: Also covers DFW area (overlapping polygon)
        DbContext.GAirmets.Add(CreateGAirmet(3, "TANGO", "TURB-LO", now,
            new[] { (-98.0, 32.0), (-96.0, 32.0), (-96.0, 34.0), (-98.0, 34.0) }));

        // G-AIRMET 4: No boundary (null area)
        DbContext.GAirmets.Add(new GAirmet
        {
            Id = 4,
            Product = "SIERRA",
            HazardType = "MT_OBSC",
            IssueTime = now,
            ExpireTime = now.AddHours(6),
            ValidTime = now.AddHours(3),
            ReceiptTime = now,
            Area = null
        });

        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task SearchAffecting_ReturnsGAirmetsContainingPoint()
    {
        var service = CreateService();

        var result = await service.SearchAffecting(DfwLat, DfwLon, null, 100, CancellationToken.None);

        result.Data.Should().HaveCount(2);
        result.Data.Select(g => g.Hazard).Should()
            .Contain(GAirmetHazardType.IFR)
            .And.Contain(GAirmetHazardType.TURB_LO);
    }

    [Fact]
    public async Task SearchAffecting_ExcludesGAirmetsNotContainingPoint()
    {
        var service = CreateService();

        var result = await service.SearchAffecting(DfwLat, DfwLon, null, 100, CancellationToken.None);

        result.Data.Select(g => g.Hazard).Should().NotContain(GAirmetHazardType.ICE);
    }

    [Fact]
    public async Task SearchAffecting_ExcludesGAirmetsWithNullBoundary()
    {
        var service = CreateService();

        var result = await service.SearchAffecting(DfwLat, DfwLon, null, 100, CancellationToken.None);

        result.Data.Select(g => g.Hazard).Should().NotContain(GAirmetHazardType.MT_OBSC);
    }

    [Fact]
    public async Task SearchAffecting_PointInHouston_ReturnsHoustonGAirmet()
    {
        var service = CreateService();

        var result = await service.SearchAffecting(29.7604m, -95.3698m, null, 100, CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Data.First().Hazard.Should().Be(GAirmetHazardType.ICE);
    }

    [Fact]
    public async Task SearchAffecting_PointOutsideAll_ReturnsEmpty()
    {
        var service = CreateService();

        var result = await service.SearchAffecting(0m, -150m, null, 100, CancellationToken.None);

        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAffecting_PaginationWorks()
    {
        var service = CreateService();

        var page1 = await service.SearchAffecting(DfwLat, DfwLon, null, 1, CancellationToken.None);
        page1.Data.Should().HaveCount(1);
        page1.Pagination.HasMore.Should().BeTrue();

        var page2 = await service.SearchAffecting(DfwLat, DfwLon, page1.Pagination.NextCursor, 1, CancellationToken.None);
        page2.Data.Should().HaveCount(1);
        page2.Pagination.HasMore.Should().BeFalse();
    }

    private GAirmetService CreateService()
    {
        return new GAirmetService(DbContext, Substitute.For<ILogger<GAirmetService>>());
    }

    private GAirmet CreateGAirmet(int id, string product, string hazardType, DateTime now,
        (double lon, double lat)[] coords)
    {
        var points = coords.Select(c => new GAirmetPoint { Longitude = c.lon, Latitude = c.lat }).ToList();

        var ringCoords = coords
            .Select(c => new Coordinate(c.lon, c.lat))
            .Append(new Coordinate(coords[0].lon, coords[0].lat))
            .ToArray();
        var polygon = _geometryFactory.CreatePolygon(ringCoords);

        return new GAirmet
        {
            Id = id,
            Product = product,
            HazardType = hazardType,
            IssueTime = now,
            ExpireTime = now.AddHours(6),
            ValidTime = now.AddHours(3),
            ReceiptTime = now,
            GeometryType = "AREA",
            Area = new GAirmetArea { NumPoints = points.Count, Points = points },
            Boundary = polygon
        };
    }
}

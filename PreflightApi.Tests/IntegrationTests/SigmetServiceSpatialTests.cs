using FluentAssertions;
using Microsoft.Extensions.Logging;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NSubstitute;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.ValueObjects.Sigmets;
using PreflightApi.Infrastructure.Services.WeatherServices;
using Xunit;

namespace PreflightApi.Tests.IntegrationTests;

[Collection("Integration")]
public class SigmetServiceSpatialTests : PostgreSqlTestBase
{
    private readonly GeometryFactory _geometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    // DFW area center
    private const decimal DfwLat = 32.8968m;
    private const decimal DfwLon = -97.0380m;

    protected override async Task SeedDatabaseAsync()
    {
        // SIGMET 1: Covers DFW area (large polygon around north Texas)
        DbContext.Sigmets.Add(CreateSigmet(1, "SIGMET CONVECTIVE over DFW",
            new[] { (-99.0, 31.0), (-95.0, 31.0), (-95.0, 35.0), (-99.0, 35.0) }));

        // SIGMET 2: Covers Houston area only (well south of DFW)
        DbContext.Sigmets.Add(CreateSigmet(2, "SIGMET ICE over Houston",
            new[] { (-96.0, 29.0), (-94.0, 29.0), (-94.0, 30.5), (-96.0, 30.5) }));

        // SIGMET 3: Also covers DFW area (overlapping polygon)
        DbContext.Sigmets.Add(CreateSigmet(3, "SIGMET TURB over DFW",
            new[] { (-98.0, 32.0), (-96.0, 32.0), (-96.0, 34.0), (-98.0, 34.0) }));

        // SIGMET 4: No boundary (null areas)
        DbContext.Sigmets.Add(new Sigmet { Id = 4, RawText = "SIGMET no area", Areas = null });

        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task SearchAffecting_ReturnsSigmetsContainingPoint()
    {
        var service = CreateService();

        var result = await service.SearchAffecting(DfwLat, DfwLon, null, 100, CancellationToken.None);

        result.Data.Should().HaveCount(2);
        result.Data.Select(s => s.RawText).Should()
            .Contain("SIGMET CONVECTIVE over DFW")
            .And.Contain("SIGMET TURB over DFW");
    }

    [Fact]
    public async Task SearchAffecting_ExcludesSigmetsNotContainingPoint()
    {
        var service = CreateService();

        var result = await service.SearchAffecting(DfwLat, DfwLon, null, 100, CancellationToken.None);

        result.Data.Select(s => s.RawText).Should().NotContain("SIGMET ICE over Houston");
    }

    [Fact]
    public async Task SearchAffecting_ExcludesSigmetsWithNullBoundary()
    {
        var service = CreateService();

        // Use a very wide search that would match if boundary existed
        var result = await service.SearchAffecting(DfwLat, DfwLon, null, 100, CancellationToken.None);

        result.Data.Select(s => s.RawText).Should().NotContain("SIGMET no area");
    }

    [Fact]
    public async Task SearchAffecting_PointInHouston_ReturnsHoustonSigmet()
    {
        var service = CreateService();

        var result = await service.SearchAffecting(29.7604m, -95.3698m, null, 100, CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Data.First().RawText.Should().Be("SIGMET ICE over Houston");
    }

    [Fact]
    public async Task SearchAffecting_PointOutsideAll_ReturnsEmpty()
    {
        var service = CreateService();

        // Middle of the Pacific
        var result = await service.SearchAffecting(0m, -150m, null, 100, CancellationToken.None);

        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchByArea_BboxOverlappingDfw_ReturnsDfwSigmets()
    {
        var service = CreateService();

        // Bounding box covering DFW area
        var result = await service.SearchByArea(32m, 34m, -98m, -96m, null, 100, CancellationToken.None);

        result.Data.Should().HaveCount(2);
        result.Data.Select(s => s.RawText).Should()
            .Contain("SIGMET CONVECTIVE over DFW")
            .And.Contain("SIGMET TURB over DFW");
    }

    [Fact]
    public async Task SearchByArea_BboxOverlappingBoth_ReturnsAll()
    {
        var service = CreateService();

        // Large bounding box covering both DFW and Houston
        var result = await service.SearchByArea(29m, 35m, -99m, -94m, null, 100, CancellationToken.None);

        result.Data.Should().HaveCount(3);
    }

    [Fact]
    public async Task SearchByArea_BboxOutsideAll_ReturnsEmpty()
    {
        var service = CreateService();

        // Bounding box in the middle of the ocean
        var result = await service.SearchByArea(-10m, -5m, -150m, -145m, null, 100, CancellationToken.None);

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

    private SigmetService CreateService()
    {
        return new SigmetService(DbContext, Substitute.For<ILogger<SigmetService>>());
    }

    private Sigmet CreateSigmet(int id, string rawText, (double lon, double lat)[] coords)
    {
        var points = coords.Select(c => new SigmetPoint { Longitude = (float)c.lon, Latitude = (float)c.lat }).ToList();

        // Build a closed polygon from the coordinates
        var ringCoords = coords
            .Select(c => new Coordinate(c.lon, c.lat))
            .Append(new Coordinate(coords[0].lon, coords[0].lat))
            .ToArray();
        var polygon = _geometryFactory.CreatePolygon(ringCoords);

        return new Sigmet
        {
            Id = id,
            RawText = rawText,
            SigmetType = "SIGMET",
            Areas = new List<SigmetArea>
            {
                new() { NumPoints = points.Count, Points = points }
            },
            Boundary = polygon
        };
    }
}

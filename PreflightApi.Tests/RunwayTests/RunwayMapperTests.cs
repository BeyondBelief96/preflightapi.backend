using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Dtos.Mappers;
using Xunit;

namespace PreflightApi.Tests.RunwayTests;

public class RunwayMapperTests
{
    private readonly GeometryFactory _geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    #region ToDto with Airport Context

    [Fact]
    public void ToDto_WithAirport_IncludesAirportContext()
    {
        var runway = CreateTestRunway();
        var airport = CreateTestAirport();

        var dto = RunwayMapper.ToDto(runway, airport, NullLogger.Instance);

        dto.AirportIcaoCode.Should().Be("KDFW");
        dto.AirportArptId.Should().Be("DFW");
        dto.AirportName.Should().Be("DALLAS-FT WORTH INTL");
        dto.RunwayId.Should().Be("17L/35R");
        dto.Length.Should().Be(13401);
        dto.Width.Should().Be(200);
    }

    [Fact]
    public void ToDto_WithAirport_IncludesRunwayEnds()
    {
        var runway = CreateTestRunway();
        runway.RunwayEnds = new List<RunwayEnd>
        {
            new() { Id = Guid.NewGuid(), RunwayEndId = "17L" },
            new() { Id = Guid.NewGuid(), RunwayEndId = "35R" }
        };
        var airport = CreateTestAirport();

        var dto = RunwayMapper.ToDto(runway, airport, NullLogger.Instance);

        dto.RunwayEnds.Should().HaveCount(2);
    }

    [Fact]
    public void ToDto_WithAirport_ExcludesGeometryByDefault()
    {
        var runway = CreateTestRunway();
        runway.Geometry = CreateTestPolygon();
        var airport = CreateTestAirport();

        var dto = RunwayMapper.ToDto(runway, airport, NullLogger.Instance);

        dto.Geometry.Should().BeNull();
    }

    [Fact]
    public void ToDto_WithAirport_IncludesGeometryWhenRequested()
    {
        var runway = CreateTestRunway();
        runway.Geometry = CreateTestPolygon();
        var airport = CreateTestAirport();

        var dto = RunwayMapper.ToDto(runway, airport, NullLogger.Instance, includeGeometry: true);

        dto.Geometry.Should().NotBeNull();
        dto.Geometry!.Type.Should().Be("Polygon");
        dto.Geometry.Coordinates.Should().HaveCount(1); // exterior ring only
        dto.Geometry.Coordinates[0].Should().HaveCount(5); // 4 corners + closing point
    }

    [Fact]
    public void ToDto_WithAirport_GeometryNull_ReturnsNullGeometryEvenIfRequested()
    {
        var runway = CreateTestRunway();
        runway.Geometry = null;
        var airport = CreateTestAirport();

        var dto = RunwayMapper.ToDto(runway, airport, NullLogger.Instance, includeGeometry: true);

        dto.Geometry.Should().BeNull();
    }

    [Fact]
    public void ToDto_WithoutAirport_HasNullAirportFields()
    {
        var runway = CreateTestRunway();

        var dto = RunwayMapper.ToDto(runway, NullLogger.Instance);

        dto.AirportIcaoCode.Should().BeNull();
        dto.AirportArptId.Should().BeNull();
        dto.AirportName.Should().BeNull();
        dto.RunwayId.Should().Be("17L/35R");
    }

    #endregion

    #region Surface Type Parsing Tests

    [Fact]
    public void ToDto_SingleSurfaceType_ParsesPrimaryOnly()
    {
        var runway = CreateTestRunway();
        runway.SurfaceTypeCode = "ASPH";

        var dto = RunwayMapper.ToDto(runway, NullLogger.Instance);

        dto.SurfaceType.Should().Be(RunwaySurfaceType.Asphalt);
        dto.SecondarySurfaceType.Should().BeNull();
    }

    [Fact]
    public void ToDto_CompositeSurfaceType_WithHyphen_ParsesBothTypes()
    {
        var runway = CreateTestRunway();
        runway.SurfaceTypeCode = "ASPH-CONC";

        var dto = RunwayMapper.ToDto(runway, NullLogger.Instance);

        dto.SurfaceType.Should().Be(RunwaySurfaceType.Asphalt);
        dto.SecondarySurfaceType.Should().Be(RunwaySurfaceType.Concrete);
    }

    [Fact]
    public void ToDto_CompositeSurfaceType_WithSlash_ParsesBothTypes()
    {
        var runway = CreateTestRunway();
        runway.SurfaceTypeCode = "CONC/ASPH";

        var dto = RunwayMapper.ToDto(runway, NullLogger.Instance);

        dto.SurfaceType.Should().Be(RunwaySurfaceType.Concrete);
        dto.SecondarySurfaceType.Should().Be(RunwaySurfaceType.Asphalt);
    }

    [Fact]
    public void ToDto_CompositeSurfaceType_GravelTurf_ParsesBothTypes()
    {
        var runway = CreateTestRunway();
        runway.SurfaceTypeCode = "GRAVEL-TURF";

        var dto = RunwayMapper.ToDto(runway, NullLogger.Instance);

        dto.SurfaceType.Should().Be(RunwaySurfaceType.Gravel);
        dto.SecondarySurfaceType.Should().Be(RunwaySurfaceType.Turf);
    }

    [Fact]
    public void ToDto_NullSurfaceType_ReturnsBothNull()
    {
        var runway = CreateTestRunway();
        runway.SurfaceTypeCode = null;

        var dto = RunwayMapper.ToDto(runway, NullLogger.Instance);

        dto.SurfaceType.Should().BeNull();
        dto.SecondarySurfaceType.Should().BeNull();
    }

    [Fact]
    public void ToDto_EmptySurfaceType_ReturnsBothNull()
    {
        var runway = CreateTestRunway();
        runway.SurfaceTypeCode = "  ";

        var dto = RunwayMapper.ToDto(runway, NullLogger.Instance);

        dto.SurfaceType.Should().BeNull();
        dto.SecondarySurfaceType.Should().BeNull();
    }

    [Fact]
    public void ToDto_CompositeSurfaceType_WithAirport_ParsesBothTypes()
    {
        var runway = CreateTestRunway();
        runway.SurfaceTypeCode = "ASPH-CONC";
        var airport = CreateTestAirport();

        var dto = RunwayMapper.ToDto(runway, airport, NullLogger.Instance);

        dto.SurfaceType.Should().Be(RunwaySurfaceType.Asphalt);
        dto.SecondarySurfaceType.Should().Be(RunwaySurfaceType.Concrete);
    }

    #endregion

    #region ToDbCode Tests

    [Theory]
    [InlineData(RunwaySurfaceType.Concrete, "CONC")]
    [InlineData(RunwaySurfaceType.Asphalt, "ASPH")]
    [InlineData(RunwaySurfaceType.Turf, "TURF")]
    [InlineData(RunwaySurfaceType.Dirt, "DIRT")]
    [InlineData(RunwaySurfaceType.Gravel, "GRAVEL")]
    [InlineData(RunwaySurfaceType.Water, "WATER")]
    [InlineData(RunwaySurfaceType.Snow, "SNOW")]
    [InlineData(RunwaySurfaceType.Ice, "ICE")]
    [InlineData(RunwaySurfaceType.Grass, "GRASS")]
    [InlineData(RunwaySurfaceType.Sand, "SAND")]
    public void ToDbCode_KnownTypes_ReturnsCorrectCode(RunwaySurfaceType surfaceType, string expectedCode)
    {
        RunwayMapper.ToDbCode(surfaceType).Should().Be(expectedCode);
    }

    #endregion

    #region ConvertToGeoJson (via ToDto)

    [Fact]
    public void ConvertToGeoJson_Polygon_ReturnsCorrectCoordinates()
    {
        var runway = CreateTestRunway();
        runway.Geometry = CreateTestPolygon();
        var airport = CreateTestAirport();

        var dto = RunwayMapper.ToDto(runway, airport, NullLogger.Instance, includeGeometry: true);

        dto.Geometry.Should().NotBeNull();
        dto.Geometry!.Type.Should().Be("Polygon");

        // Verify first coordinate
        var firstCoord = dto.Geometry.Coordinates[0][0];
        firstCoord[0].Should().BeApproximately(-97.04, 0.01); // longitude (X)
        firstCoord[1].Should().BeApproximately(32.89, 0.01);  // latitude (Y)
    }

    [Fact]
    public void ConvertToGeoJson_PolygonWithHole_IncludesInteriorRings()
    {
        var exteriorCoords = new Coordinate[]
        {
            new(-97.05, 32.89),
            new(-97.03, 32.89),
            new(-97.03, 32.88),
            new(-97.05, 32.88),
            new(-97.05, 32.89)
        };
        var interiorCoords = new Coordinate[]
        {
            new(-97.045, 32.888),
            new(-97.035, 32.888),
            new(-97.035, 32.882),
            new(-97.045, 32.882),
            new(-97.045, 32.888)
        };

        var polygon = _geometryFactory.CreatePolygon(
            _geometryFactory.CreateLinearRing(exteriorCoords),
            new[] { _geometryFactory.CreateLinearRing(interiorCoords) });

        var runway = CreateTestRunway();
        runway.Geometry = polygon;
        var airport = CreateTestAirport();

        var dto = RunwayMapper.ToDto(runway, airport, NullLogger.Instance, includeGeometry: true);

        dto.Geometry!.Coordinates.Should().HaveCount(2); // exterior + 1 interior ring
    }

    #endregion

    private static Runway CreateTestRunway() => new()
    {
        Id = Guid.NewGuid(),
        SiteNo = "50078.*A",
        RunwayId = "17L/35R",
        Length = 13401,
        Width = 200,
        SurfaceTypeCode = "CONC",
        EdgeLightIntensity = "HIGH",
        RunwayEnds = new List<RunwayEnd>()
    };

    private static Airport CreateTestAirport() => new()
    {
        SiteNo = "50078.*A",
        IcaoId = "KDFW",
        ArptId = "DFW",
        ArptName = "DALLAS-FT WORTH INTL"
    };

    private Geometry CreateTestPolygon()
    {
        var coords = new Coordinate[]
        {
            new(-97.04, 32.89),
            new(-97.03, 32.89),
            new(-97.03, 32.88),
            new(-97.04, 32.88),
            new(-97.04, 32.89) // closing point
        };
        return _geometryFactory.CreatePolygon(_geometryFactory.CreateLinearRing(coords));
    }
}

using System.Text.Json;
using FluentAssertions;
using NetTopologySuite.Geometries;
using PreflightApi.Infrastructure.Dtos.Notam;
using PreflightApi.Infrastructure.Services.NotamServices;
using Xunit;

namespace PreflightApi.Tests.NotamTests;

public class NotamGeometryParserTests
{
    [Fact]
    public void Parse_ShouldReturnNull_WhenGeometryIsNull()
    {
        var result = NotamGeometryParser.Parse(null);
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_ShouldReturnNull_WhenTypeIsUnknown()
    {
        var geometry = new NotamGeometryDto { Type = "MultiLineString" };
        var result = NotamGeometryParser.Parse(geometry);
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_ShouldParsePoint()
    {
        // Arrange — Point coordinates as JsonElement
        var coordsJson = "[-97.038, 32.897]";
        var coords = JsonSerializer.Deserialize<JsonElement>(coordsJson);
        var geometry = new NotamGeometryDto { Type = "Point", Coordinates = coords };

        // Act
        var result = NotamGeometryParser.Parse(geometry);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<Point>();
        var point = (Point)result!;
        point.X.Should().BeApproximately(-97.038, 0.001);
        point.Y.Should().BeApproximately(32.897, 0.001);
        point.SRID.Should().Be(4326);
    }

    [Fact]
    public void Parse_ShouldReturnNull_WhenPointCoordinatesAreNull()
    {
        var geometry = new NotamGeometryDto { Type = "Point", Coordinates = null };
        var result = NotamGeometryParser.Parse(geometry);
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_ShouldParsePolygon()
    {
        // Arrange — simple square polygon
        var coordsJson = "[[[-97.0, 32.0], [-97.0, 33.0], [-96.0, 33.0], [-96.0, 32.0], [-97.0, 32.0]]]";
        var coords = JsonSerializer.Deserialize<JsonElement>(coordsJson);
        var geometry = new NotamGeometryDto { Type = "Polygon", Coordinates = coords };

        // Act
        var result = NotamGeometryParser.Parse(geometry);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<Polygon>();
        var polygon = (Polygon)result!;
        polygon.ExteriorRing.NumPoints.Should().Be(5);
        polygon.SRID.Should().Be(4326);
    }

    [Fact]
    public void Parse_ShouldReturnNull_WhenPolygonCoordinatesAreNull()
    {
        var geometry = new NotamGeometryDto { Type = "Polygon", Coordinates = null };
        var result = NotamGeometryParser.Parse(geometry);
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_ShouldParseGeometryCollection()
    {
        // Arrange
        var pointCoordsJson = "[-97.038, 32.897]";
        var pointCoords = JsonSerializer.Deserialize<JsonElement>(pointCoordsJson);

        var polygonCoordsJson = "[[[-97.0, 32.0], [-97.0, 33.0], [-96.0, 33.0], [-96.0, 32.0], [-97.0, 32.0]]]";
        var polygonCoords = JsonSerializer.Deserialize<JsonElement>(polygonCoordsJson);

        var geometry = new NotamGeometryDto
        {
            Type = "GeometryCollection",
            Geometries =
            [
                new NotamGeometryDto { Type = "Point", Coordinates = pointCoords },
                new NotamGeometryDto { Type = "Polygon", Coordinates = polygonCoords }
            ]
        };

        // Act
        var result = NotamGeometryParser.Parse(geometry);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<GeometryCollection>();
        var collection = (GeometryCollection)result!;
        collection.NumGeometries.Should().Be(2);
        collection.Geometries[0].Should().BeOfType<Point>();
        collection.Geometries[1].Should().BeOfType<Polygon>();
    }

    [Fact]
    public void Parse_ShouldReturnNull_WhenGeometryCollectionIsEmpty()
    {
        var geometry = new NotamGeometryDto
        {
            Type = "GeometryCollection",
            Geometries = []
        };

        var result = NotamGeometryParser.Parse(geometry);
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_ShouldHandleMalformedCoordinates_Gracefully()
    {
        // Arrange — coordinates that aren't a valid array
        var badCoordsJson = "\"not-an-array\"";
        var badCoords = JsonSerializer.Deserialize<JsonElement>(badCoordsJson);
        var geometry = new NotamGeometryDto { Type = "Point", Coordinates = badCoords };

        // Act
        var result = NotamGeometryParser.Parse(geometry);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_ShouldHandleInsufficientPointCoordinates()
    {
        // Arrange — only one coordinate
        var coordsJson = "[-97.038]";
        var coords = JsonSerializer.Deserialize<JsonElement>(coordsJson);
        var geometry = new NotamGeometryDto { Type = "Point", Coordinates = coords };

        // Act
        var result = NotamGeometryParser.Parse(geometry);

        // Assert
        result.Should().BeNull();
    }
}

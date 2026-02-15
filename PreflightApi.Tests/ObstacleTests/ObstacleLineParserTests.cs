using System.Text;
using FluentAssertions;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using PreflightApi.Infrastructure.Utilities;
using Xunit;

namespace PreflightApi.Tests.ObstacleTests;

public class ObstacleLineParserTests
{
    private readonly GeometryFactory _geometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    /// <summary>
    /// Builds a fixed-width DOF line with fields at the correct positions.
    /// </summary>
    private static string BuildDofLine(
        string oasCode = "06",
        string obstacleNumber = "123456",
        string verificationStatus = "O",
        string country = "US",
        string state = "TX",
        string city = "DALLAS",
        string latDeg = "32",
        string latMin = "53",
        string latSec = "48.50",
        string latHemi = "N",
        string lonDeg = "097",
        string lonMin = "02",
        string lonSec = "16.80",
        string lonHemi = "W",
        string type = "TOWER",
        string quantity = "1",
        string heightAgl = "1500",
        string heightAmsl = "2000",
        string lighting = "R",
        string hAccuracy = "1",
        string vAccuracy = "A",
        string mark = " ",
        string faaStudy = "2024-OE-12345",
        string action = "A",
        string julianDate = "2024001")
    {
        var sb = new StringBuilder(128);
        sb.Append(oasCode.PadRight(2));       // 0-1
        sb.Append(' ');                        // 2
        sb.Append(obstacleNumber.PadRight(6)); // 3-8
        sb.Append(' ');                        // 9
        sb.Append(verificationStatus.PadRight(1)); // 10
        sb.Append(' ');                        // 11
        sb.Append(country.PadRight(2));        // 12-13
        sb.Append(' ');                        // 14
        sb.Append(state.PadRight(2));          // 15-16
        sb.Append(' ');                        // 17
        sb.Append(city.PadRight(16));          // 18-33
        sb.Append(' ');                        // 34
        sb.Append(latDeg.PadLeft(2));          // 35-36
        sb.Append(' ');                        // 37
        sb.Append(latMin.PadLeft(2));          // 38-39
        sb.Append(' ');                        // 40
        sb.Append(latSec.PadLeft(5));          // 41-45
        sb.Append(latHemi);                    // 46
        sb.Append(' ');                        // 47
        sb.Append(lonDeg.PadLeft(3));          // 48-50
        sb.Append(' ');                        // 51
        sb.Append(lonMin.PadLeft(2));          // 52-53
        sb.Append(' ');                        // 54
        sb.Append(lonSec.PadLeft(5));          // 55-59
        sb.Append(lonHemi);                    // 60
        sb.Append(' ');                        // 61
        sb.Append(type.PadRight(18));          // 62-79
        sb.Append(' ');                        // 80
        sb.Append(quantity.PadLeft(1));        // 81
        sb.Append(' ');                        // 82
        sb.Append(heightAgl.PadLeft(5));       // 83-87
        sb.Append(' ');                        // 88
        sb.Append(heightAmsl.PadLeft(5));      // 89-93
        sb.Append(' ');                        // 94
        sb.Append(lighting.PadRight(1));       // 95
        sb.Append(' ');                        // 96
        sb.Append(hAccuracy.PadRight(1));      // 97
        sb.Append(' ');                        // 98
        sb.Append(vAccuracy.PadRight(1));      // 99
        sb.Append(' ');                        // 100
        sb.Append(mark.PadRight(1));           // 101
        sb.Append(' ');                        // 102
        sb.Append(faaStudy.PadRight(14));      // 103-116
        sb.Append(' ');                        // 117
        sb.Append(action.PadRight(1));         // 118
        sb.Append(' ');                        // 119
        sb.Append(julianDate.PadRight(7));     // 120-126
        return sb.ToString();
    }

    [Fact]
    public void ParseObstacleLine_ValidLine_ParsesAllFields()
    {
        var line = BuildDofLine();

        var result = ObstacleLineParser.ParseObstacleLine(line, _geometryFactory);

        result.Should().NotBeNull();
        result!.OasCode.Should().Be("06");
        result.ObstacleNumber.Should().Be("123456");
        result.OasNumber.Should().Be("06-123456");
        result.VerificationStatus.Should().Be("O");
        result.CountryId.Should().Be("US");
        result.StateId.Should().Be("TX");
        result.CityName.Should().Be("DALLAS");
        result.LatDegrees.Should().Be(32);
        result.LatMinutes.Should().Be(53);
        result.LatSeconds.Should().Be(48.50m);
        result.LatHemisphere.Should().Be("N");
        result.LongDegrees.Should().Be(97);
        result.LongMinutes.Should().Be(2);
        result.LongSeconds.Should().Be(16.80m);
        result.LongHemisphere.Should().Be("W");
        result.ObstacleType.Should().Be("TOWER");
        result.Quantity.Should().Be(1);
        result.HeightAgl.Should().Be(1500);
        result.HeightAmsl.Should().Be(2000);
        result.Lighting.Should().Be("R");
        result.HorizontalAccuracy.Should().Be("1");
        result.VerticalAccuracy.Should().Be("A");
        result.FaaStudyNumber.Should().Be("2024-OE-12345");
        result.Action.Should().Be("A");
        result.JulianDate.Should().Be("2024001");
        result.Location.Should().NotBeNull();
    }

    [Fact]
    public void ParseObstacleLine_ValidLine_CalculatesCorrectDecimalCoordinates()
    {
        var line = BuildDofLine(
            latDeg: "32", latMin: "53", latSec: "48.50", latHemi: "N",
            lonDeg: "097", lonMin: "02", lonSec: "16.80", lonHemi: "W");

        var result = ObstacleLineParser.ParseObstacleLine(line, _geometryFactory);

        result.Should().NotBeNull();
        // 32 + 53/60 + 48.50/3600 ≈ 32.89680556
        result!.LatDecimal.Should().BeApproximately(32.8968m, 0.001m);
        // -(97 + 2/60 + 16.80/3600) ≈ -97.03800
        result.LongDecimal.Should().BeApproximately(-97.0380m, 0.001m);
    }

    [Fact]
    public void ParseObstacleLine_SouthernHemisphere_NegativeLatitude()
    {
        var line = BuildDofLine(latHemi: "S");

        var result = ObstacleLineParser.ParseObstacleLine(line, _geometryFactory);

        result.Should().NotBeNull();
        result!.LatDecimal.Should().BeNegative();
    }

    [Fact]
    public void ParseObstacleLine_EasternHemisphere_PositiveLongitude()
    {
        var line = BuildDofLine(lonHemi: "E");

        var result = ObstacleLineParser.ParseObstacleLine(line, _geometryFactory);

        result.Should().NotBeNull();
        result!.LongDecimal.Should().BePositive();
    }

    [Fact]
    public void ParseObstacleLine_NullLine_ReturnsNull()
    {
        var result = ObstacleLineParser.ParseObstacleLine(null!, _geometryFactory);

        result.Should().BeNull();
    }

    [Fact]
    public void ParseObstacleLine_EmptyLine_ReturnsNull()
    {
        var result = ObstacleLineParser.ParseObstacleLine("", _geometryFactory);

        result.Should().BeNull();
    }

    [Fact]
    public void ParseObstacleLine_WhitespaceLine_ReturnsNull()
    {
        var result = ObstacleLineParser.ParseObstacleLine("   ", _geometryFactory);

        result.Should().BeNull();
    }

    [Fact]
    public void ParseObstacleLine_ShortLine_ReturnsNull()
    {
        var result = ObstacleLineParser.ParseObstacleLine(new string('X', 99), _geometryFactory);

        result.Should().BeNull();
    }

    [Fact]
    public void ParseObstacleLine_InvalidLatitude_ReturnsNull()
    {
        var line = BuildDofLine(latDeg: "XX");

        var result = ObstacleLineParser.ParseObstacleLine(line, _geometryFactory);

        result.Should().BeNull();
    }

    [Fact]
    public void ParseObstacleLine_InvalidLongitude_ReturnsNull()
    {
        var line = BuildDofLine(lonDeg: "XXX");

        var result = ObstacleLineParser.ParseObstacleLine(line, _geometryFactory);

        result.Should().BeNull();
    }

    [Fact]
    public void ParseObstacleLine_ZeroQuantity_ReturnsNullQuantity()
    {
        var line = BuildDofLine(quantity: "0");

        var result = ObstacleLineParser.ParseObstacleLine(line, _geometryFactory);

        result.Should().NotBeNull();
        result!.Quantity.Should().BeNull();
    }

    [Fact]
    public void ParseObstacleLine_ZeroHeightAgl_ReturnsNullHeightAgl()
    {
        var line = BuildDofLine(heightAgl: "0");

        var result = ObstacleLineParser.ParseObstacleLine(line, _geometryFactory);

        result.Should().NotBeNull();
        result!.HeightAgl.Should().BeNull();
    }

    [Fact]
    public void ParseObstacleLine_LocationUsesLongLatOrder()
    {
        var line = BuildDofLine(
            latDeg: "32", latMin: "53", latSec: "48.50", latHemi: "N",
            lonDeg: "097", lonMin: "02", lonSec: "16.80", lonHemi: "W");

        var result = ObstacleLineParser.ParseObstacleLine(line, _geometryFactory);

        result.Should().NotBeNull();
        // NTS Point uses (X=Longitude, Y=Latitude)
        result!.Location!.X.Should().BeApproximately((double)result.LongDecimal!, 0.001);
        result.Location.Y.Should().BeApproximately((double)result.LatDecimal!, 0.001);
    }

    [Fact]
    public void ConvertDmsToDecimal_NorthernHemisphere_ReturnsPositive()
    {
        var result = ObstacleLineParser.ConvertDmsToDecimal(32, 53, 48.50m, "N");

        result.Should().BeApproximately(32.8968m, 0.001m);
    }

    [Fact]
    public void ConvertDmsToDecimal_SouthernHemisphere_ReturnsNegative()
    {
        var result = ObstacleLineParser.ConvertDmsToDecimal(32, 53, 48.50m, "S");

        result.Should().BeApproximately(-32.8968m, 0.001m);
    }

    [Fact]
    public void ConvertDmsToDecimal_WesternHemisphere_ReturnsNegative()
    {
        var result = ObstacleLineParser.ConvertDmsToDecimal(97, 2, 16.80m, "W");

        result.Should().BeApproximately(-97.0380m, 0.001m);
    }

    [Fact]
    public void ConvertDmsToDecimal_EasternHemisphere_ReturnsPositive()
    {
        var result = ObstacleLineParser.ConvertDmsToDecimal(97, 2, 16.80m, "E");

        result.Should().BeApproximately(97.0380m, 0.001m);
    }

    [Fact]
    public void SafeSubstring_ValidRange_ReturnsSubstring()
    {
        var result = ObstacleLineParser.SafeSubstring("Hello World", 0, 5);

        result.Should().Be("Hello");
    }

    [Fact]
    public void SafeSubstring_StartBeyondLength_ReturnsEmpty()
    {
        var result = ObstacleLineParser.SafeSubstring("Hello", 10, 5);

        result.Should().BeEmpty();
    }

    [Fact]
    public void SafeSubstring_LengthExceedsRemaining_ReturnsTruncated()
    {
        var result = ObstacleLineParser.SafeSubstring("Hello", 3, 10);

        result.Should().Be("lo");
    }
}

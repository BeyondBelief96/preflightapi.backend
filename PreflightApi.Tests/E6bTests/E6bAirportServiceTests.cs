using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Performance;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Services;
using Xunit;

namespace PreflightApi.Tests.E6bTests;

/// <summary>
/// Tests for E6bCalculatorService airport-based async methods:
/// GetCrosswindForAirportAsync and GetDensityAltitudeForAirportAsync.
/// Uses in-memory database seeded with Airport + Runway + RunwayEnd entities.
/// </summary>
public class E6bAirportServiceTests : IDisposable
{
    private readonly PreflightApiDbContext _dbContext;
    private readonly IMetarService _metarService;
    private readonly E6bCalculatorService _service;

    private const string TestSiteNo = "50082.*A";
    private const string TestIcaoId = "KDFW";
    private const string TestArptId = "DFW";

    public E6bAirportServiceTests()
    {
        var options = new DbContextOptionsBuilder<PreflightApiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PreflightApiDbContext(options);
        _metarService = Substitute.For<IMetarService>();
        var logger = Substitute.For<ILogger<E6bCalculatorService>>();

        _service = new E6bCalculatorService(_dbContext, _metarService, logger);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetCrosswindForAirportAsync Tests

    [Fact]
    public async Task GetCrosswind_ShouldReturnCrosswindData_ForValidAirport()
    {
        // Arrange
        SeedAirportWithRunways(TestSiteNo, TestIcaoId, TestArptId, elevation: 607m,
            magVarn: 4m, magHemis: "E",
            runwayEnds: [("18L", 180), ("36R", 360)]);

        _metarService.GetMetarForAirport(Arg.Any<string>()).Returns(new MetarDto
        {
            WindDirDegrees = "200",
            WindSpeedKt = 15,
            RawText = "KDFW 221853Z 20015KT 10SM CLR 30/10 A2992"
        });

        // Act
        var result = await _service.GetCrosswindForAirportAsync("KDFW");

        // Assert
        result.Should().NotBeNull();
        result.AirportIdentifier.Should().Be("KDFW");
        result.WindSpeedKt.Should().Be(15);
        result.WindDirectionDegrees.Should().Be(200);
        result.IsVariableWind.Should().BeFalse();
        result.Runways.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetCrosswind_ShouldThrowAirportNotFoundException_WhenAirportNotFound()
    {
        // Arrange — no airport seeded
        _metarService.GetMetarForAirport(Arg.Any<string>()).Returns(new MetarDto());

        // Act
        var act = () => _service.GetCrosswindForAirportAsync("KXYZ");

        // Assert
        await act.Should().ThrowAsync<AirportNotFoundException>();
    }

    [Fact]
    public async Task GetCrosswind_ShouldThrowWeatherDataMissingException_WhenNoWindSpeed()
    {
        // Arrange
        SeedAirportWithRunways(TestSiteNo, TestIcaoId, TestArptId, elevation: 607m,
            runwayEnds: [("18L", 180)]);

        _metarService.GetMetarForAirport(Arg.Any<string>()).Returns(new MetarDto
        {
            WindDirDegrees = "200",
            WindSpeedKt = null // missing
        });

        // Act
        var act = () => _service.GetCrosswindForAirportAsync("KDFW");

        // Assert
        await act.Should().ThrowAsync<WeatherDataMissingException>();
    }

    [Fact]
    public async Task GetCrosswind_ShouldHandleVariableWind_FullCrosswind()
    {
        // Arrange
        SeedAirportWithRunways(TestSiteNo, TestIcaoId, TestArptId, elevation: 607m,
            runwayEnds: [("18L", 180)]);

        _metarService.GetMetarForAirport(Arg.Any<string>()).Returns(new MetarDto
        {
            WindDirDegrees = "VRB",
            WindSpeedKt = 10
        });

        // Act
        var result = await _service.GetCrosswindForAirportAsync("KDFW");

        // Assert — VRB means full crosswind = wind speed
        result.IsVariableWind.Should().BeTrue();
        result.WindDirectionDegrees.Should().BeNull();
        result.Runways.Should().ContainSingle();
        result.Runways[0].AbsoluteCrosswindKt.Should().Be(10);
    }

    [Fact]
    public async Task GetCrosswind_ShouldSkipRunwayEnds_WithoutTrueAlignment()
    {
        // Arrange — seed both runway ends upfront, one with null TrueAlignment
        SeedAirportWithRunways(TestSiteNo, TestIcaoId, TestArptId, elevation: 607m,
            runwayEnds: [("18L", 180)],
            nullAlignmentEnds: ["36R"]);

        _metarService.GetMetarForAirport(Arg.Any<string>()).Returns(new MetarDto
        {
            WindDirDegrees = "200",
            WindSpeedKt = 15
        });

        // Act
        var result = await _service.GetCrosswindForAirportAsync("KDFW");

        // Assert — only the runway end with alignment should appear
        result.Runways.Should().HaveCount(1);
        result.Runways[0].RunwayEndId.Should().Be("18L");
    }

    [Fact]
    public async Task GetCrosswind_ShouldRecommendRunway_WithLowestCrosswindAndHeadwind()
    {
        // Arrange — wind from 190°, runway 18 (180°) has ~10° angle, runway 09 (090°) has ~100° angle
        SeedAirportWithRunways(TestSiteNo, TestIcaoId, TestArptId, elevation: 607m,
            magVarn: 0m, magHemis: "E",
            runwayEnds: [("18L", 180), ("09R", 90)]);

        _metarService.GetMetarForAirport(Arg.Any<string>()).Returns(new MetarDto
        {
            WindDirDegrees = "190",
            WindSpeedKt = 15
        });

        // Act
        var result = await _service.GetCrosswindForAirportAsync("KDFW");

        // Assert — 18L should be recommended (smallest crosswind + headwind)
        result.RecommendedRunway.Should().Be("18L");
    }

    [Fact]
    public async Task GetCrosswind_ShouldConvertTrueToMagnetic_EastVariation()
    {
        // Arrange — East variation: magnetic = true - variation
        SeedAirportWithRunways(TestSiteNo, TestIcaoId, TestArptId, elevation: 607m,
            magVarn: 10m, magHemis: "E",
            runwayEnds: [("36L", 360)]);

        _metarService.GetMetarForAirport(Arg.Any<string>()).Returns(new MetarDto
        {
            WindDirDegrees = "360",
            WindSpeedKt = 10
        });

        // Act
        var result = await _service.GetCrosswindForAirportAsync("KDFW");

        // Assert — true 360 with 10°E variation → magnetic 350
        result.Runways.Should().ContainSingle();
        result.Runways[0].MagneticHeadingDegrees.Should().Be(350);
    }

    [Fact]
    public async Task GetCrosswind_ShouldConvertTrueToMagnetic_WestVariation()
    {
        // Arrange — West variation: magnetic = true + variation
        SeedAirportWithRunways(TestSiteNo, TestIcaoId, TestArptId, elevation: 607m,
            magVarn: 10m, magHemis: "W",
            runwayEnds: [("36L", 360)]);

        _metarService.GetMetarForAirport(Arg.Any<string>()).Returns(new MetarDto
        {
            WindDirDegrees = "360",
            WindSpeedKt = 10
        });

        // Act
        var result = await _service.GetCrosswindForAirportAsync("KDFW");

        // Assert — true 360 with 10°W variation → magnetic 10 (360+10 → normalized)
        result.Runways.Should().ContainSingle();
        result.Runways[0].MagneticHeadingDegrees.Should().Be(10);
    }

    [Fact]
    public async Task GetCrosswind_ShouldHandleGustSpeed()
    {
        // Arrange
        SeedAirportWithRunways(TestSiteNo, TestIcaoId, TestArptId, elevation: 607m,
            magVarn: 0m, magHemis: "E",
            runwayEnds: [("18L", 180)]);

        _metarService.GetMetarForAirport(Arg.Any<string>()).Returns(new MetarDto
        {
            WindDirDegrees = "270",
            WindSpeedKt = 15,
            WindGustKt = 25
        });

        // Act
        var result = await _service.GetCrosswindForAirportAsync("KDFW");

        // Assert — gust crosswind should be calculated
        result.WindGustKt.Should().Be(25);
        result.Runways.Should().ContainSingle();
        result.Runways[0].GustCrosswindKt.Should().NotBeNull();
        // 90° angle (270 wind on 180 runway), full crosswind
        Math.Abs(result.Runways[0].GustCrosswindKt!.Value).Should().BeApproximately(25, 0.5);
    }

    [Fact]
    public async Task GetCrosswind_ShouldMatchByArptId()
    {
        // Arrange — query by FAA identifier
        SeedAirportWithRunways(TestSiteNo, TestIcaoId, TestArptId, elevation: 607m,
            runwayEnds: [("18L", 180)]);

        _metarService.GetMetarForAirport(Arg.Any<string>()).Returns(new MetarDto
        {
            WindDirDegrees = "200",
            WindSpeedKt = 15
        });

        // Act
        var result = await _service.GetCrosswindForAirportAsync("DFW");

        // Assert
        result.Should().NotBeNull();
        result.AirportIdentifier.Should().Be("KDFW");
    }

    #endregion

    #region GetDensityAltitudeForAirportAsync Tests

    [Fact]
    public async Task GetDensityAltitude_ShouldReturnDensityAltitude_FromMetar()
    {
        // Arrange
        SeedAirport(TestSiteNo, TestIcaoId, TestArptId, elevation: 607m);

        _metarService.GetMetarForAirport(Arg.Any<string>()).Returns(new MetarDto
        {
            TempC = 30f,
            AltimInHg = 29.92f,
            RawText = "KDFW 221853Z 20015KT 10SM CLR 30/10 A2992"
        });

        // Act
        var result = await _service.GetDensityAltitudeForAirportAsync("KDFW");

        // Assert
        result.Should().NotBeNull();
        result.AirportIdentifier.Should().Be("KDFW");
        result.FieldElevationFt.Should().Be(607);
        result.DensityAltitudeFt.Should().BeGreaterThan(607); // hot day → DA > field elev
    }

    [Fact]
    public async Task GetDensityAltitude_ShouldThrowAirportNotFoundException_WhenAirportNotFound()
    {
        // Arrange — no airport seeded
        _metarService.GetMetarForAirport(Arg.Any<string>()).Returns(new MetarDto());

        // Act
        var act = () => _service.GetDensityAltitudeForAirportAsync("KXYZ");

        // Assert
        await act.Should().ThrowAsync<AirportNotFoundException>();
    }

    [Fact]
    public async Task GetDensityAltitude_ShouldThrowInvalidPerformanceDataException_WhenNoElevation()
    {
        // Arrange — airport with null elevation
        SeedAirport(TestSiteNo, TestIcaoId, TestArptId, elevation: null);

        _metarService.GetMetarForAirport(Arg.Any<string>()).Returns(new MetarDto
        {
            TempC = 25f,
            AltimInHg = 29.92f
        });

        // Act
        var act = () => _service.GetDensityAltitudeForAirportAsync("KDFW");

        // Assert
        await act.Should().ThrowAsync<InvalidPerformanceDataException>();
    }

    [Fact]
    public async Task GetDensityAltitude_ShouldThrowWeatherDataMissingException_WhenNoTemperature()
    {
        // Arrange
        SeedAirport(TestSiteNo, TestIcaoId, TestArptId, elevation: 607m);

        _metarService.GetMetarForAirport(Arg.Any<string>()).Returns(new MetarDto
        {
            TempC = null, // missing
            AltimInHg = 29.92f
        });

        // Act — no override provided
        var act = () => _service.GetDensityAltitudeForAirportAsync("KDFW");

        // Assert
        await act.Should().ThrowAsync<WeatherDataMissingException>();
    }

    [Fact]
    public async Task GetDensityAltitude_ShouldThrowWeatherDataMissingException_WhenNoAltimeter()
    {
        // Arrange
        SeedAirport(TestSiteNo, TestIcaoId, TestArptId, elevation: 607m);

        _metarService.GetMetarForAirport(Arg.Any<string>()).Returns(new MetarDto
        {
            TempC = 25f,
            AltimInHg = null // missing
        });

        // Act — no override provided
        var act = () => _service.GetDensityAltitudeForAirportAsync("KDFW");

        // Assert
        await act.Should().ThrowAsync<WeatherDataMissingException>();
    }

    [Fact]
    public async Task GetDensityAltitude_ShouldUseTemperatureOverride_WhenProvided()
    {
        // Arrange
        SeedAirport(TestSiteNo, TestIcaoId, TestArptId, elevation: 607m);

        _metarService.GetMetarForAirport(Arg.Any<string>()).Returns(new MetarDto
        {
            TempC = 20f, // METAR temp
            AltimInHg = 29.92f
        });

        var request = new AirportDensityAltitudeRequestDto
        {
            TemperatureCelsiusOverride = 40.0 // override with hotter temp
        };

        // Act
        var result = await _service.GetDensityAltitudeForAirportAsync("KDFW", request);

        // Assert — override temp used, so DA should reflect 40°C not 20°C
        result.ActualTemperatureCelsius.Should().Be(40.0);
    }

    [Fact]
    public async Task GetDensityAltitude_ShouldUseAltimeterOverride_WhenProvided()
    {
        // Arrange
        SeedAirport(TestSiteNo, TestIcaoId, TestArptId, elevation: 607m);

        _metarService.GetMetarForAirport(Arg.Any<string>()).Returns(new MetarDto
        {
            TempC = 25f,
            AltimInHg = 29.92f // METAR altimeter
        });

        var request = new AirportDensityAltitudeRequestDto
        {
            AltimeterInHgOverride = 28.50 // lower pressure override
        };

        // Act
        var result = await _service.GetDensityAltitudeForAirportAsync("KDFW", request);

        // Assert — override altimeter used
        result.AltimeterInHg.Should().Be(28.50);
        result.PressureAltitudeFt.Should().BeGreaterThan(607); // lower pressure → higher PA
    }

    [Fact]
    public async Task GetDensityAltitude_ShouldCalculateCorrectDensityAltitude_HotDay()
    {
        // Arrange — hot Texas summer day: 40°C, standard pressure, 607ft elevation
        SeedAirport(TestSiteNo, TestIcaoId, TestArptId, elevation: 607m);

        _metarService.GetMetarForAirport(Arg.Any<string>()).Returns(new MetarDto
        {
            TempC = 40f,
            AltimInHg = 29.92f
        });

        // Act
        var result = await _service.GetDensityAltitudeForAirportAsync("KDFW");

        // Assert
        // PA = 607 + (29.92 - 29.92)*1000 = 607
        // ISA temp at PA=607: 15 - (607/1000)*2 = 13.786
        // Temp deviation: 40 - 13.786 = 26.214
        // DA = 607 + 120*26.214 = 607 + 3145.7 ≈ 3753
        result.PressureAltitudeFt.Should().Be(607);
        result.DensityAltitudeFt.Should().BeApproximately(3753, 5);
        result.TemperatureDeviationCelsius.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetDensityAltitude_ShouldCalculateCorrectDensityAltitude_StandardDay()
    {
        // Arrange — ISA conditions at sea level: 15°C, 29.92 inHg, 0 ft
        SeedAirport(TestSiteNo, TestIcaoId, TestArptId, elevation: 0m);

        _metarService.GetMetarForAirport(Arg.Any<string>()).Returns(new MetarDto
        {
            TempC = 15f,
            AltimInHg = 29.92f
        });

        // Act
        var result = await _service.GetDensityAltitudeForAirportAsync("KDFW");

        // Assert — at ISA, DA ≈ field elevation
        result.PressureAltitudeFt.Should().Be(0);
        result.DensityAltitudeFt.Should().BeApproximately(0, 5);
        result.TemperatureDeviationCelsius.Should().BeApproximately(0, 0.5);
    }

    #endregion

    #region Helpers

    private void SeedAirport(string siteNo, string icaoId, string arptId, decimal? elevation)
    {
        _dbContext.Airports.Add(new Airport
        {
            SiteNo = siteNo,
            IcaoId = icaoId,
            ArptId = arptId,
            Elev = elevation
        });
        _dbContext.SaveChanges();
    }

    private void SeedAirportWithRunways(
        string siteNo,
        string icaoId,
        string arptId,
        decimal elevation,
        decimal? magVarn = null,
        string? magHemis = null,
        (string endId, int trueAlignment)[]? runwayEnds = null,
        string[]? nullAlignmentEnds = null)
    {
        var airport = new Airport
        {
            SiteNo = siteNo,
            IcaoId = icaoId,
            ArptId = arptId,
            Elev = elevation,
            MagVarn = magVarn,
            MagHemis = magHemis
        };
        _dbContext.Airports.Add(airport);

        var allEnds = new List<RunwayEnd>();

        if (runwayEnds != null)
        {
            allEnds.AddRange(runwayEnds.Select(re => new RunwayEnd
            {
                Id = Guid.NewGuid(),
                SiteNo = siteNo,
                RunwayIdRef = "18/36",
                RunwayEndId = re.endId,
                TrueAlignment = re.trueAlignment
            }));
        }

        if (nullAlignmentEnds != null)
        {
            allEnds.AddRange(nullAlignmentEnds.Select(endId => new RunwayEnd
            {
                Id = Guid.NewGuid(),
                SiteNo = siteNo,
                RunwayIdRef = "18/36",
                RunwayEndId = endId,
                TrueAlignment = null
            }));
        }

        if (allEnds.Count > 0)
        {
            var runway = new Runway
            {
                Id = Guid.NewGuid(),
                SiteNo = siteNo,
                RunwayId = "18/36",
                RunwayEnds = allEnds
            };
            _dbContext.Runways.Add(runway);
        }

        _dbContext.SaveChanges();
    }

    #endregion
}

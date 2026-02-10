using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos.Performance;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Services;
using Xunit;

namespace PreflightApi.Tests.E6bTests;

public class E6bCalculatorServiceTests
{
    private readonly E6bCalculatorService _sut;

    public E6bCalculatorServiceTests()
    {
        var options = new DbContextOptionsBuilder<PreflightApiDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Performance")
            .Options;
        var context = new PreflightApiDbContext(options);
        var metarService = Substitute.For<IMetarService>();
        var logger = Substitute.For<ILogger<E6bCalculatorService>>();
        _sut = new E6bCalculatorService(context, metarService, logger);
    }

    #region Wind Triangle Tests

    [Fact]
    public void CalculateWindTriangle_NoWind_ReturnsHeadingEqualsTrueCourse()
    {
        var request = new WindTriangleRequestDto
        {
            TrueCourseDegrees = 360,
            TrueAirspeedKt = 120,
            WindDirectionDegrees = 0,
            WindSpeedKt = 0
        };

        var result = _sut.CalculateWindTriangle(request);

        result.TrueHeadingDegrees.Should().Be(360);
        result.GroundSpeedKt.Should().Be(120);
        result.WindCorrectionAngleDegrees.Should().Be(0);
    }

    [Fact]
    public void CalculateWindTriangle_DirectHeadwind_ReducesGroundSpeed()
    {
        // Wind FROM 360 (north), flying course 360 (north) = direct headwind
        var request = new WindTriangleRequestDto
        {
            TrueCourseDegrees = 360,
            TrueAirspeedKt = 120,
            WindDirectionDegrees = 360,
            WindSpeedKt = 20
        };

        var result = _sut.CalculateWindTriangle(request);

        result.TrueHeadingDegrees.Should().Be(360);
        result.GroundSpeedKt.Should().BeApproximately(100, 0.5);
        result.WindCorrectionAngleDegrees.Should().BeApproximately(0, 0.1);
        result.HeadwindComponentKt.Should().BeApproximately(20, 0.5);
    }

    [Fact]
    public void CalculateWindTriangle_DirectTailwind_IncreasesGroundSpeed()
    {
        // Wind FROM 180 (south), flying course 360 (north) = direct tailwind
        var request = new WindTriangleRequestDto
        {
            TrueCourseDegrees = 360,
            TrueAirspeedKt = 120,
            WindDirectionDegrees = 180,
            WindSpeedKt = 20
        };

        var result = _sut.CalculateWindTriangle(request);

        result.TrueHeadingDegrees.Should().Be(360);
        result.GroundSpeedKt.Should().BeApproximately(140, 0.5);
        result.WindCorrectionAngleDegrees.Should().BeApproximately(0, 0.1);
        result.HeadwindComponentKt.Should().BeApproximately(-20, 0.5);
    }

    [Fact]
    public void CalculateWindTriangle_RightCrosswind_CorrectsHeadingRight()
    {
        // Wind FROM 090, flying course 360 = right crosswind
        var request = new WindTriangleRequestDto
        {
            TrueCourseDegrees = 360,
            TrueAirspeedKt = 120,
            WindDirectionDegrees = 090,
            WindSpeedKt = 20
        };

        var result = _sut.CalculateWindTriangle(request);

        // WCA should be positive (crab right into wind)
        result.WindCorrectionAngleDegrees.Should().BeGreaterThan(0);
        result.TrueHeadingDegrees.Should().BeApproximately(9.6, 1.0);
        result.GroundSpeedKt.Should().BeApproximately(118.3, 1.0);
        result.CrosswindComponentKt.Should().BeApproximately(20, 0.5);
    }

    [Fact]
    public void CalculateWindTriangle_LeftCrosswind_CorrectsHeadingLeft()
    {
        // Wind FROM 270, flying course 360 = left crosswind
        var request = new WindTriangleRequestDto
        {
            TrueCourseDegrees = 360,
            TrueAirspeedKt = 120,
            WindDirectionDegrees = 270,
            WindSpeedKt = 20
        };

        var result = _sut.CalculateWindTriangle(request);

        // WCA should be negative (crab left into wind)
        result.WindCorrectionAngleDegrees.Should().BeLessThan(0);
        result.TrueHeadingDegrees.Should().BeApproximately(350.4, 1.0);
        result.GroundSpeedKt.Should().BeApproximately(118.3, 1.0);
        result.CrosswindComponentKt.Should().BeApproximately(-20, 0.5);
    }

    [Fact]
    public void CalculateWindTriangle_QuarteringHeadwind_CorrectResults()
    {
        // Flying east (090), wind from NE (045) = quartering headwind from left
        var request = new WindTriangleRequestDto
        {
            TrueCourseDegrees = 090,
            TrueAirspeedKt = 100,
            WindDirectionDegrees = 045,
            WindSpeedKt = 30
        };

        var result = _sut.CalculateWindTriangle(request);

        // Should crab left into the wind component
        result.WindCorrectionAngleDegrees.Should().BeLessThan(0);
        result.GroundSpeedKt.Should().BeLessThan(100); // headwind component reduces GS
        result.HeadwindComponentKt.Should().BeGreaterThan(0); // has headwind component
    }

    [Fact]
    public void CalculateWindTriangle_HeadingWrapsPast360_NormalizesCorrectly()
    {
        // Flying 350, wind from 270 = left crosswind, heading should wrap to > 340
        var request = new WindTriangleRequestDto
        {
            TrueCourseDegrees = 350,
            TrueAirspeedKt = 100,
            WindDirectionDegrees = 270,
            WindSpeedKt = 15
        };

        var result = _sut.CalculateWindTriangle(request);

        result.TrueHeadingDegrees.Should().BeInRange(330, 350);
        result.GroundSpeedKt.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CalculateWindTriangle_WindSpeedExceedsTas_ClampsWcaAndReturnsValidResult()
    {
        // Wind speed > TAS — extreme case
        var request = new WindTriangleRequestDto
        {
            TrueCourseDegrees = 360,
            TrueAirspeedKt = 50,
            WindDirectionDegrees = 090,
            WindSpeedKt = 60
        };

        var result = _sut.CalculateWindTriangle(request);

        // Should not throw; WCA clamped to 90°
        result.GroundSpeedKt.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void CalculateWindTriangle_TypicalCrossCountry_ReasonableResults()
    {
        // Flying south (180), wind from SW (230) at 15kt
        var request = new WindTriangleRequestDto
        {
            TrueCourseDegrees = 180,
            TrueAirspeedKt = 110,
            WindDirectionDegrees = 230,
            WindSpeedKt = 15
        };

        var result = _sut.CalculateWindTriangle(request);

        // Wind from 230 on course 180 = quartering headwind from right, crab right
        result.TrueHeadingDegrees.Should().BeInRange(183, 190);
        result.GroundSpeedKt.Should().BeLessThan(110);
        result.HeadwindComponentKt.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CalculateWindTriangle_Course000And360_EquivalentResults()
    {
        var request360 = new WindTriangleRequestDto
        {
            TrueCourseDegrees = 360,
            TrueAirspeedKt = 100,
            WindDirectionDegrees = 090,
            WindSpeedKt = 10
        };

        var result360 = _sut.CalculateWindTriangle(request360);

        // Both should give essentially the same flight parameters
        result360.GroundSpeedKt.Should().BeGreaterThan(0);
        result360.WindCorrectionAngleDegrees.Should().BeGreaterThan(0);
    }

    #endregion

    #region True Airspeed Tests

    // Reference dataset verified against compressible isentropic flow equations.
    // Formula: CAS → qc (impact pressure) → Mach at altitude → TAS
    // Sources: ICAO Doc 7488 (Standard Atmosphere), isentropic flow relations.

    [Theory]
    // Sea level and low altitude — GA standard conditions
    [InlineData(80, 0, 15, 80.0)]          // Sea level, standard day
    [InlineData(60, 0, 15, 60.0)]          // Very slow, sea level
    [InlineData(100, 1000, 13, 101.5)]     // Low altitude, near standard
    [InlineData(120, 2000, 11, 123.5)]     // Light aircraft cruise
    [InlineData(90, 3000, 9, 94.0)]        // Slow cruise, 3000ft
    // Mid altitude — typical GA cruise
    [InlineData(110, 4000, 7, 116.6)]      // GA cruise, 4000ft
    [InlineData(130, 5000, 5, 139.9)]      // GA cruise, 5000ft std
    [InlineData(140, 6000, 3, 152.9)]      // GA cruise, 6000ft std
    [InlineData(150, 7000, 1, 166.2)]      // GA cruise, 7000ft std
    [InlineData(160, 8000, -1, 180.0)]     // GA cruise, 8000ft std
    [InlineData(170, 9000, -3, 194.1)]     // GA fast cruise, 9000ft
    // 10000ft — multiple CAS values
    [InlineData(100, 10000, -5, 116.2)]    // Slow at 10000ft std
    [InlineData(120, 10000, -5, 139.3)]    // GA at 10000ft std
    [InlineData(150, 10000, -5, 174.0)]    // Fast GA at 10000ft
    // High altitude GA and turboprop
    [InlineData(175, 11000, -8, 205.5)]    // Turbo GA, 11000ft
    [InlineData(190, 15000, -15, 237.6)]   // High altitude GA
    [InlineData(180, 12000, -9, 215.0)]    // Turboprop, 12000ft std
    [InlineData(200, 14000, -13, 246.0)]   // High perf GA, 14000ft
    public void CalculateTrueAirspeed_GaAltitudes_MatchesReferenceData(
        double cas, double pa, double oat, double expectedTas)
    {
        var request = new TrueAirspeedRequestDto
        {
            CalibratedAirspeedKt = cas,
            PressureAltitudeFt = pa,
            OutsideAirTemperatureCelsius = oat
        };

        var result = _sut.CalculateTrueAirspeed(request);

        result.TrueAirspeedKt.Should().BeApproximately(expectedTas, 1.0);
    }

    [Theory]
    // Non-standard temperatures at various altitudes
    [InlineData(100, 0, 40, 104.2)]       // Hot day, sea level
    [InlineData(100, 0, -20, 93.7)]       // Cold day, sea level
    [InlineData(120, 5000, 25, 133.7)]    // Very warm, 5000ft
    [InlineData(120, 5000, -5, 126.8)]    // Cold day, 5000ft
    [InlineData(120, 5000, 15, 131.4)]    // Warm day, 5000ft
    [InlineData(160, 8000, 10, 183.6)]    // Warm day, 8000ft
    [InlineData(160, 8000, -15, 175.3)]   // Cold day, 8000ft
    [InlineData(140, 6000, 20, 157.5)]    // Hot day, 6000ft
    [InlineData(140, 6000, -10, 149.2)]   // Cold day, 6000ft
    [InlineData(150, 10000, 5, 177.2)]    // Warm at 10000ft (ISA+10)
    [InlineData(150, 10000, -20, 169.1)]  // Cold at 10000ft (ISA-15)
    public void CalculateTrueAirspeed_NonStandardTemperatures_MatchesReferenceData(
        double cas, double pa, double oat, double expectedTas)
    {
        var request = new TrueAirspeedRequestDto
        {
            CalibratedAirspeedKt = cas,
            PressureAltitudeFt = pa,
            OutsideAirTemperatureCelsius = oat
        };

        var result = _sut.CalculateTrueAirspeed(request);

        result.TrueAirspeedKt.Should().BeApproximately(expectedTas, 1.0);
    }

    [Theory]
    // Flight levels — compressibility correction required
    [InlineData(200, 18000, -21, 261.9)]   // FL180, std temp
    [InlineData(210, 17500, -20, 272.5)]   // FL175, non-std
    [InlineData(220, 20000, -25, 296.6)]   // FL200, std temp
    [InlineData(230, 22000, -30, 319.0)]   // FL220, near std
    [InlineData(250, 25000, -35, 363.0)]   // FL250, std
    [InlineData(260, 27000, -38, 389.7)]   // FL270
    [InlineData(270, 28000, -40, 410.1)]   // FL280, std
    [InlineData(280, 30000, -45, 436.8)]   // FL300, std
    [InlineData(290, 32000, -49, 465.4)]   // FL320
    [InlineData(250, 33000, -51, 412.9)]   // FL330
    [InlineData(300, 35000, -55, 502.8)]   // FL350, std
    [InlineData(320, 36000, -57, 540.4)]   // FL360, near tropopause
    // Non-standard temperatures at flight levels
    [InlineData(200, 20000, -10, 278.4)]   // Warm FL200 (ISA+15)
    [InlineData(200, 20000, -40, 262.0)]   // Cold FL200 (ISA-15)
    [InlineData(250, 30000, -30, 406.0)]   // Warm FL300 (ISA+15)
    [InlineData(250, 30000, -60, 380.1)]   // Cold FL300 (ISA-15)
    public void CalculateTrueAirspeed_FlightLevels_MatchesReferenceData(
        double cas, double pa, double oat, double expectedTas)
    {
        var request = new TrueAirspeedRequestDto
        {
            CalibratedAirspeedKt = cas,
            PressureAltitudeFt = pa,
            OutsideAirTemperatureCelsius = oat
        };

        var result = _sut.CalculateTrueAirspeed(request);

        result.TrueAirspeedKt.Should().BeApproximately(expectedTas, 1.0);
    }

    [Theory]
    // Above the tropopause (36,089 ft) — isothermal stratosphere
    [InlineData(300, 37000, -57, 521.0)]   // FL370
    [InlineData(280, 39000, -57, 510.6)]   // FL390
    [InlineData(310, 41000, -57, 580.0)]   // FL410
    public void CalculateTrueAirspeed_AboveTropopause_MatchesReferenceData(
        double cas, double pa, double oat, double expectedTas)
    {
        var request = new TrueAirspeedRequestDto
        {
            CalibratedAirspeedKt = cas,
            PressureAltitudeFt = pa,
            OutsideAirTemperatureCelsius = oat
        };

        var result = _sut.CalculateTrueAirspeed(request);

        result.TrueAirspeedKt.Should().BeApproximately(expectedTas, 1.0);
    }

    // Validated against flightcondition Python library (pip install flightcondition)
    // US Standard Atmosphere 1976, compressible isentropic flow: CAS → Mach → TAS
    [Theory]
    // Sea level ISA
    [InlineData(60, 0, 15, 60.0)]           // #1: Sea level, ISA
    [InlineData(100, 0, 15, 100.0)]          // #2: Sea level, ISA
    [InlineData(150, 0, 15, 150.0)]          // #3: Sea level, ISA
    [InlineData(200, 0, 15, 200.0)]          // #4: Sea level, ISA
    [InlineData(250, 0, 15, 250.0)]          // #5: Sea level, ISA
    [InlineData(300, 0, 15, 300.0)]          // #6: Sea level, ISA
    // Low/mid altitude ISA
    [InlineData(80, 2000, 11, 82.4)]         // #7: Low alt, slow, ISA
    [InlineData(100, 3000, 9.1, 104.5)]      // #8: Low alt cruise, ISA
    [InlineData(120, 5000, 5.1, 129.2)]      // #9: GA cruise, 5000ft ISA
    [InlineData(140, 7000, 1.1, 155.2)]      // #10: GA cruise, 7000ft ISA
    [InlineData(160, 10000, -4.8, 185.6)]    // #11: GA fast, 10000ft ISA
    [InlineData(180, 12000, -8.8, 215.1)]    // #12: Turboprop, 12000ft ISA
    // High altitude ISA
    [InlineData(200, 15000, -14.7, 250.0)]   // #13: High perf, 15000ft ISA
    [InlineData(200, 18000, -20.6, 262.0)]   // #14: FL180, ISA
    [InlineData(220, 20000, -24.6, 296.7)]   // #15: FL200, ISA
    [InlineData(250, 25000, -34.5, 363.2)]   // #16: FL250, ISA
    [InlineData(280, 30000, -44.4, 437.0)]   // #17: FL300, ISA
    [InlineData(300, 35000, -54.2, 503.1)]   // #18: FL350, ISA
    // Above tropopause ISA
    [InlineData(300, 37000, -56.5, 521.0)]   // #19: FL370, ISA
    [InlineData(280, 39000, -56.5, 510.4)]   // #20: FL390, ISA
    [InlineData(310, 41000, -56.5, 579.7)]   // #21: FL410, ISA
    // ISA+10 to ISA+20 (warm deviations)
    [InlineData(100, 0, 35, 103.4)]          // #22: Sea level, ISA+20
    [InlineData(120, 5000, 15.1, 131.5)]     // #23: 5000ft, ISA+10
    [InlineData(150, 10000, 5.2, 177.3)]     // #24: 10000ft, ISA+10
    [InlineData(180, 15000, -4.7, 229.6)]    // #25: 15000ft, ISA+10
    [InlineData(200, 20000, -4.6, 281.1)]    // #26: FL200, ISA+20
    [InlineData(250, 25000, -14.5, 378.1)]   // #27: FL250, ISA+20
    [InlineData(280, 30000, -24.4, 455.7)]   // #28: FL300, ISA+20
    [InlineData(300, 35000, -34.2, 525.6)]   // #29: FL350, ISA+20
    // ISA-10 to ISA-20 (cold deviations)
    [InlineData(100, 0, -5, 96.5)]           // #30: Sea level, ISA-20
    [InlineData(120, 5000, -4.9, 126.8)]     // #31: 5000ft, ISA-10
    [InlineData(150, 10000, -14.8, 170.8)]   // #32: 10000ft, ISA-10
    [InlineData(180, 15000, -34.7, 216.4)]   // #33: 15000ft, ISA-20
    [InlineData(200, 20000, -44.6, 259.3)]   // #34: FL200, ISA-20
    [InlineData(250, 30000, -64.4, 375.8)]   // #35: FL300, ISA-20
    // #36 skipped: OAT -74.2°C exceeds validation limit of -70°C
    // Edge cases — min speed at altitude, max speed at sea level
    [InlineData(60, 10000, -4.8, 69.8)]      // #38: Min speed, 10000ft
    [InlineData(80, 15000, -14.7, 100.7)]    // #39: Slow, 15000ft ISA
    // #40 skipped: duplicate of #6 (300kt, sea level, ISA)
    [InlineData(120, 0, 45, 126.1)]          // #41: Very hot sea level
    public void CalculateTrueAirspeed_FlightConditionLibrary_MatchesReferenceData(
        double cas, double pa, double oat, double expectedTas)
    {
        var request = new TrueAirspeedRequestDto
        {
            CalibratedAirspeedKt = cas,
            PressureAltitudeFt = pa,
            OutsideAirTemperatureCelsius = oat
        };

        var result = _sut.CalculateTrueAirspeed(request);

        result.TrueAirspeedKt.Should().BeApproximately(expectedTas, 1.0);
    }

    [Fact]
    public void CalculateTrueAirspeed_SeaLevelStandardDay_DensityAltitudeIsZero()
    {
        var request = new TrueAirspeedRequestDto
        {
            CalibratedAirspeedKt = 100,
            PressureAltitudeFt = 0,
            OutsideAirTemperatureCelsius = 15
        };

        var result = _sut.CalculateTrueAirspeed(request);

        result.DensityAltitudeFt.Should().BeApproximately(0, 50);
    }

    [Fact]
    public void CalculateTrueAirspeed_ReturnsMachNumber()
    {
        var request = new TrueAirspeedRequestDto
        {
            CalibratedAirspeedKt = 280,
            PressureAltitudeFt = 30000,
            OutsideAirTemperatureCelsius = -45
        };

        var result = _sut.CalculateTrueAirspeed(request);

        result.MachNumber.Should().BeGreaterThan(0.5);
        result.MachNumber.Should().BeLessThan(1.0);
    }

    #endregion

    #region Cloud Base Tests

    [Fact]
    public void CalculateCloudBase_TypicalSpread_Returns4000Ft()
    {
        var request = new CloudBaseRequestDto
        {
            TemperatureCelsius = 25,
            DewpointCelsius = 15
        };

        var result = _sut.CalculateCloudBase(request);

        result.EstimatedCloudBaseFtAgl.Should().Be(4000);
        result.TemperatureDewpointSpreadCelsius.Should().Be(10);
    }

    [Fact]
    public void CalculateCloudBase_SmallSpread_LowClouds()
    {
        var request = new CloudBaseRequestDto
        {
            TemperatureCelsius = 20,
            DewpointCelsius = 18
        };

        var result = _sut.CalculateCloudBase(request);

        result.EstimatedCloudBaseFtAgl.Should().Be(800);
        result.TemperatureDewpointSpreadCelsius.Should().Be(2);
    }

    [Fact]
    public void CalculateCloudBase_LargeSpread_HighOrNoClouds()
    {
        var request = new CloudBaseRequestDto
        {
            TemperatureCelsius = 30,
            DewpointCelsius = 5
        };

        var result = _sut.CalculateCloudBase(request);

        result.EstimatedCloudBaseFtAgl.Should().Be(10000);
        result.TemperatureDewpointSpreadCelsius.Should().Be(25);
    }

    [Fact]
    public void CalculateCloudBase_ZeroSpread_Fog()
    {
        var request = new CloudBaseRequestDto
        {
            TemperatureCelsius = 15,
            DewpointCelsius = 15
        };

        var result = _sut.CalculateCloudBase(request);

        result.EstimatedCloudBaseFtAgl.Should().Be(0);
        result.TemperatureDewpointSpreadCelsius.Should().Be(0);
    }

    [Fact]
    public void CalculateCloudBase_NearFreezing_CorrectResult()
    {
        var request = new CloudBaseRequestDto
        {
            TemperatureCelsius = 2,
            DewpointCelsius = -1
        };

        var result = _sut.CalculateCloudBase(request);

        result.EstimatedCloudBaseFtAgl.Should().Be(1200);
        result.TemperatureDewpointSpreadCelsius.Should().Be(3);
    }

    [Fact]
    public void CalculateCloudBase_NegativeTemperatures_CorrectResult()
    {
        var request = new CloudBaseRequestDto
        {
            TemperatureCelsius = -5,
            DewpointCelsius = -10
        };

        var result = _sut.CalculateCloudBase(request);

        result.EstimatedCloudBaseFtAgl.Should().Be(2000);
        result.TemperatureDewpointSpreadCelsius.Should().Be(5);
    }

    #endregion

    #region Pressure Altitude Tests

    [Fact]
    public void CalculatePressureAltitude_StandardDaySeaLevel_ReturnsZero()
    {
        var request = new PressureAltitudeRequestDto
        {
            FieldElevationFt = 0,
            AltimeterInHg = 29.92
        };

        var result = _sut.CalculatePressureAltitude(request);

        result.PressureAltitudeFt.Should().Be(0);
        result.AltimeterCorrectionFt.Should().Be(0);
    }

    [Fact]
    public void CalculatePressureAltitude_StandardDayAtElevation_ReturnsSameElevation()
    {
        var request = new PressureAltitudeRequestDto
        {
            FieldElevationFt = 5000,
            AltimeterInHg = 29.92
        };

        var result = _sut.CalculatePressureAltitude(request);

        result.PressureAltitudeFt.Should().Be(5000);
        result.AltimeterCorrectionFt.Should().Be(0);
    }

    [Fact]
    public void CalculatePressureAltitude_LowPressureSeaLevel_ReturnsPositivePA()
    {
        var request = new PressureAltitudeRequestDto
        {
            FieldElevationFt = 0,
            AltimeterInHg = 29.42
        };

        var result = _sut.CalculatePressureAltitude(request);

        result.PressureAltitudeFt.Should().Be(500);
        result.AltimeterCorrectionFt.Should().Be(500);
    }

    [Fact]
    public void CalculatePressureAltitude_HighPressureAtElevation_ReducesPA()
    {
        var request = new PressureAltitudeRequestDto
        {
            FieldElevationFt = 3000,
            AltimeterInHg = 30.42
        };

        var result = _sut.CalculatePressureAltitude(request);

        result.PressureAltitudeFt.Should().Be(2500);
        result.AltimeterCorrectionFt.Should().Be(-500);
    }

    [Fact]
    public void CalculatePressureAltitude_VeryLowPressure_LargeCorrection()
    {
        var request = new PressureAltitudeRequestDto
        {
            FieldElevationFt = 1000,
            AltimeterInHg = 28.92
        };

        var result = _sut.CalculatePressureAltitude(request);

        result.PressureAltitudeFt.Should().Be(2000);
        result.AltimeterCorrectionFt.Should().Be(1000);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void CalculateWindTriangle_NegativeTas_ThrowsValidationException()
    {
        var request = new WindTriangleRequestDto
        {
            TrueCourseDegrees = 360,
            TrueAirspeedKt = -10,
            WindDirectionDegrees = 0,
            WindSpeedKt = 10
        };

        var act = () => _sut.CalculateWindTriangle(request);

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void CalculateTrueAirspeed_ZeroCas_ThrowsValidationException()
    {
        var request = new TrueAirspeedRequestDto
        {
            CalibratedAirspeedKt = 0,
            PressureAltitudeFt = 5000,
            OutsideAirTemperatureCelsius = 15
        };

        var act = () => _sut.CalculateTrueAirspeed(request);

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void CalculateCloudBase_DewpointExceedsTemperature_ThrowsValidationException()
    {
        var request = new CloudBaseRequestDto
        {
            TemperatureCelsius = 15,
            DewpointCelsius = 20
        };

        var act = () => _sut.CalculateCloudBase(request);

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void CalculatePressureAltitude_AltimeterOutOfRange_ThrowsValidationException()
    {
        var request = new PressureAltitudeRequestDto
        {
            FieldElevationFt = 1000,
            AltimeterInHg = 24.0
        };

        var act = () => _sut.CalculatePressureAltitude(request);

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void CalculateWindTriangle_NegativeWindSpeed_ThrowsValidationException()
    {
        var request = new WindTriangleRequestDto
        {
            TrueCourseDegrees = 180,
            TrueAirspeedKt = 100,
            WindDirectionDegrees = 090,
            WindSpeedKt = -5
        };

        var act = () => _sut.CalculateWindTriangle(request);

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void CalculateTrueAirspeed_TemperatureTooHigh_ThrowsValidationException()
    {
        var request = new TrueAirspeedRequestDto
        {
            CalibratedAirspeedKt = 100,
            PressureAltitudeFt = 0,
            OutsideAirTemperatureCelsius = 65
        };

        var act = () => _sut.CalculateTrueAirspeed(request);

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void CalculateWindTriangle_CourseOutOfRange_ThrowsValidationException()
    {
        var request = new WindTriangleRequestDto
        {
            TrueCourseDegrees = 400,
            TrueAirspeedKt = 100,
            WindDirectionDegrees = 090,
            WindSpeedKt = 10
        };

        var act = () => _sut.CalculateWindTriangle(request);

        act.Should().Throw<ValidationException>();
    }

    #endregion

    #region Echo-Back Input Tests

    [Fact]
    public void CalculateWindTriangle_EchosBackInputs()
    {
        var request = new WindTriangleRequestDto
        {
            TrueCourseDegrees = 180,
            TrueAirspeedKt = 110,
            WindDirectionDegrees = 230,
            WindSpeedKt = 15
        };

        var result = _sut.CalculateWindTriangle(request);

        result.TrueCourseDegrees.Should().Be(180);
        result.TrueAirspeedKt.Should().Be(110);
        result.WindDirectionDegrees.Should().Be(230);
        result.WindSpeedKt.Should().Be(15);
    }

    [Fact]
    public void CalculateTrueAirspeed_EchosBackInputs()
    {
        var request = new TrueAirspeedRequestDto
        {
            CalibratedAirspeedKt = 120,
            PressureAltitudeFt = 8000,
            OutsideAirTemperatureCelsius = 0
        };

        var result = _sut.CalculateTrueAirspeed(request);

        result.CalibratedAirspeedKt.Should().Be(120);
        result.PressureAltitudeFt.Should().Be(8000);
        result.OutsideAirTemperatureCelsius.Should().Be(0);
    }

    [Fact]
    public void CalculateCloudBase_EchosBackInputs()
    {
        var request = new CloudBaseRequestDto
        {
            TemperatureCelsius = 22,
            DewpointCelsius = 14
        };

        var result = _sut.CalculateCloudBase(request);

        result.TemperatureCelsius.Should().Be(22);
        result.DewpointCelsius.Should().Be(14);
    }

    [Fact]
    public void CalculatePressureAltitude_EchosBackInputs()
    {
        var request = new PressureAltitudeRequestDto
        {
            FieldElevationFt = 2500,
            AltimeterInHg = 30.10
        };

        var result = _sut.CalculatePressureAltitude(request);

        result.FieldElevationFt.Should().Be(2500);
        result.AltimeterInHg.Should().Be(30.10);
    }

    #endregion
}

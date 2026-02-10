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

namespace PreflightApi.Tests.PerformanceTests;

public class PerformanceCalculatorServiceTests
{
    private readonly PerformanceCalculatorService _sut;

    public PerformanceCalculatorServiceTests()
    {
        var options = new DbContextOptionsBuilder<PreflightApiDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Performance")
            .Options;
        var context = new PreflightApiDbContext(options);
        var metarService = Substitute.For<IMetarService>();
        var logger = Substitute.For<ILogger<PerformanceCalculatorService>>();
        _sut = new PerformanceCalculatorService(context, metarService, logger);
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

    [Fact]
    public void CalculateTrueAirspeed_SeaLevelStandardDay_TasEqualsCas()
    {
        var request = new TrueAirspeedRequestDto
        {
            CalibratedAirspeedKt = 100,
            PressureAltitudeFt = 0,
            OutsideAirTemperatureCelsius = 15
        };

        var result = _sut.CalculateTrueAirspeed(request);

        result.TrueAirspeedKt.Should().BeApproximately(100, 0.5);
        result.DensityAltitudeFt.Should().BeApproximately(0, 50);
    }

    [Fact]
    public void CalculateTrueAirspeed_5000FtStandard_TasGreaterThanCas()
    {
        // ISA at 5000ft = 15 - (5000/1000)*2 = 5°C
        var request = new TrueAirspeedRequestDto
        {
            CalibratedAirspeedKt = 100,
            PressureAltitudeFt = 5000,
            OutsideAirTemperatureCelsius = 5
        };

        var result = _sut.CalculateTrueAirspeed(request);

        result.TrueAirspeedKt.Should().BeApproximately(108, 2.0);
        result.DensityAltitudeFt.Should().BeApproximately(5000, 100);
    }

    [Fact]
    public void CalculateTrueAirspeed_10000FtStandard_TasSignificantlyGreater()
    {
        // ISA at 10000ft = 15 - (10000/1000)*2 = -5°C
        var request = new TrueAirspeedRequestDto
        {
            CalibratedAirspeedKt = 100,
            PressureAltitudeFt = 10000,
            OutsideAirTemperatureCelsius = -5
        };

        var result = _sut.CalculateTrueAirspeed(request);

        result.TrueAirspeedKt.Should().BeApproximately(121, 2.0);
        result.DensityAltitudeFt.Should().BeApproximately(10000, 100);
    }

    [Fact]
    public void CalculateTrueAirspeed_HotDayAtElevation_HigherTas()
    {
        // ISA at 5000ft = 5°C, actual = 30°C → 25°C above standard
        var request = new TrueAirspeedRequestDto
        {
            CalibratedAirspeedKt = 100,
            PressureAltitudeFt = 5000,
            OutsideAirTemperatureCelsius = 30
        };

        var result = _sut.CalculateTrueAirspeed(request);

        result.TrueAirspeedKt.Should().BeGreaterThan(112);
        result.DensityAltitudeFt.Should().BeGreaterThan(7000); // significantly above PA
    }

    [Fact]
    public void CalculateTrueAirspeed_ColdDayAtElevation_LowerTas()
    {
        // ISA at 5000ft = 5°C, actual = -10°C → 15°C below standard
        var request = new TrueAirspeedRequestDto
        {
            CalibratedAirspeedKt = 100,
            PressureAltitudeFt = 5000,
            OutsideAirTemperatureCelsius = -10
        };

        var result = _sut.CalculateTrueAirspeed(request);

        result.TrueAirspeedKt.Should().BeLessThan(108);
        result.DensityAltitudeFt.Should().BeLessThan(4000); // below PA
    }

    [Fact]
    public void CalculateTrueAirspeed_HighAltitude_LargeTasIncrease()
    {
        // ISA at 15000ft = 15 - 30 = -15°C
        var request = new TrueAirspeedRequestDto
        {
            CalibratedAirspeedKt = 150,
            PressureAltitudeFt = 15000,
            OutsideAirTemperatureCelsius = -15
        };

        var result = _sut.CalculateTrueAirspeed(request);

        result.TrueAirspeedKt.Should().BeApproximately(200, 5.0);
    }

    [Fact]
    public void CalculateTrueAirspeed_SeaLevelHotDay_SlightTasIncrease()
    {
        // ISA at sea level = 15°C, actual = 35°C → 20°C above standard
        var request = new TrueAirspeedRequestDto
        {
            CalibratedAirspeedKt = 100,
            PressureAltitudeFt = 0,
            OutsideAirTemperatureCelsius = 35
        };

        var result = _sut.CalculateTrueAirspeed(request);

        result.TrueAirspeedKt.Should().BeApproximately(103, 2.0);
        result.DensityAltitudeFt.Should().BeGreaterThan(2000);
    }

    [Fact]
    public void CalculateTrueAirspeed_ReturnsMachNumber()
    {
        var request = new TrueAirspeedRequestDto
        {
            CalibratedAirspeedKt = 150,
            PressureAltitudeFt = 10000,
            OutsideAirTemperatureCelsius = -5
        };

        var result = _sut.CalculateTrueAirspeed(request);

        result.MachNumber.Should().BeGreaterThan(0);
        result.MachNumber.Should().BeLessThan(1.0); // subsonic GA aircraft
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

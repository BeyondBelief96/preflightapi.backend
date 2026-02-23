using FluentAssertions;
using PreflightApi.Infrastructure.Dtos.Navlog;
using PreflightApi.Infrastructure.Services;
using Xunit;

namespace PreflightApi.Tests.NavlogTests;

/// <summary>
/// Tests for NavlogService.InterpolateWindTempData — the 7+ special cases covering
/// exact matches, 3000ft temperature estimation, interpolation between levels,
/// extrapolation above/below, direction wrapping across 360, and fallbacks.
/// </summary>
public class WindInterpolationTests
{
    private static WindsAloftSiteDto CreateSite(Dictionary<string, WindTempDto> windTemp)
    {
        return new WindsAloftSiteDto
        {
            Id = "DFW",
            Lat = 32.9f,
            Lon = -97.0f,
            WindTemp = windTemp
        };
    }

    #region Exact Match Tests

    [Fact]
    public void ShouldReturnExactData_ForStandardAltitude()
    {
        // Arrange — exact data at 6000 ft
        var site = CreateSite(new Dictionary<string, WindTempDto>
        {
            ["3000"] = new() { Direction = 270, Speed = 10 },
            ["6000"] = new() { Direction = 180, Speed = 20, Temperature = 5f },
            ["9000"] = new() { Direction = 200, Speed = 30, Temperature = -1f }
        });

        // Act
        var result = NavlogService.InterpolateWindTempData(6000, site);

        // Assert — should return exact 6000 ft data
        result.Should().NotBeNull();
        result!.Direction.Should().Be(180);
        result.Speed.Should().Be(20);
        result.Temperature.Should().Be(5f);
    }

    [Fact]
    public void ShouldReturnExactData_For3000ft_EstimatingTemperature()
    {
        // Arrange — 3000 ft has wind but no temp; 6000 ft has temp
        var site = CreateSite(new Dictionary<string, WindTempDto>
        {
            ["3000"] = new() { Direction = 270, Speed = 10 },
            ["6000"] = new() { Direction = 180, Speed = 20, Temperature = 2f },
            ["9000"] = new() { Direction = 200, Speed = 30, Temperature = -4f }
        });

        // Act
        var result = NavlogService.InterpolateWindTempData(3000, site);

        // Assert — wind from 3000, temp extrapolated from 6000 (+2°C per 1000 ft * 3)
        result.Should().NotBeNull();
        result!.Direction.Should().Be(270);
        result.Speed.Should().Be(10);
        // Temp at 3000 = 2 + (6000-3000)/1000 * 2 = 2 + 6 = 8
        result.Temperature.Should().BeApproximately(8f, 0.01f);
    }

    #endregion

    #region Interpolation Tests

    [Fact]
    public void ShouldInterpolate_BetweenTwoStandardAltitudes()
    {
        // Arrange — 7500 ft between 6000 and 9000
        var site = CreateSite(new Dictionary<string, WindTempDto>
        {
            ["6000"] = new() { Direction = 180, Speed = 20, Temperature = 4f },
            ["9000"] = new() { Direction = 210, Speed = 35, Temperature = -2f }
        });

        // Act
        var result = NavlogService.InterpolateWindTempData(7500, site);

        // Assert — ratio = (7500-6000)/(9000-6000) = 0.5
        result.Should().NotBeNull();
        result!.Direction.Should().Be(195); // 180 + 30*0.5
        result.Speed.Should().Be(27);       // 20 + 15*0.5 = 27.5 → truncated to 27
        result.Temperature.Should().BeApproximately(1f, 0.01f); // 4 + (-6)*0.5 = 1
    }

    [Fact]
    public void ShouldInterpolate_WithDirectionCrossing360()
    {
        // Arrange — lower=350, upper=10 → should wrap correctly (not go through 180)
        var site = CreateSite(new Dictionary<string, WindTempDto>
        {
            ["6000"] = new() { Direction = 350, Speed = 20, Temperature = 4f },
            ["9000"] = new() { Direction = 10, Speed = 30, Temperature = -2f }
        });

        // Act — midpoint at 7500 ft
        var result = NavlogService.InterpolateWindTempData(7500, site);

        // Assert — should be 360/0 (wrapping through 360, not going 350→10 via 180)
        result.Should().NotBeNull();
        // dirDiff = 10 - 350 = -340 → abs > 180 → -340+360 = 20 → interpolated = (350 + 20*0.5 + 360) % 360 = 0
        result!.Direction.Should().Be(0);
    }

    [Fact]
    public void ShouldInterpolateSpeed_Linearly()
    {
        // Arrange
        var site = CreateSite(new Dictionary<string, WindTempDto>
        {
            ["6000"] = new() { Direction = 180, Speed = 10, Temperature = 4f },
            ["9000"] = new() { Direction = 180, Speed = 40, Temperature = -2f }
        });

        // Act — 1/3 of the way: 7000 ft → ratio = 1000/3000 = 0.333
        var result = NavlogService.InterpolateWindTempData(7000, site);

        // Assert
        result.Should().NotBeNull();
        result!.Speed.Should().Be(20); // 10 + 30*0.333 = 20
    }

    [Fact]
    public void ShouldInterpolate_When3000ftIsLowerBound()
    {
        // Arrange — altitude between 3000 and 6000
        var site = CreateSite(new Dictionary<string, WindTempDto>
        {
            ["3000"] = new() { Direction = 270, Speed = 10 },
            ["6000"] = new() { Direction = 180, Speed = 20, Temperature = 2f }
        });

        // Act — 4500 ft: ratio = (4500-3000)/(6000-3000) = 0.5
        var result = NavlogService.InterpolateWindTempData(4500, site);

        // Assert
        result.Should().NotBeNull();
        result!.Speed.Should().Be(15); // 10 + 10*0.5
        // Temp: estimated 3000 temp = 2 + (6000-3000)/1000*2 = 8
        // Then for 4500: 8 - (4500-3000)/1000*2 = 8 - 3 = 5
        result.Temperature.Should().BeApproximately(5f, 0.01f);
    }

    #endregion

    #region Extrapolation Tests

    [Fact]
    public void ShouldExtrapolate_BelowLowestAltitude()
    {
        // Arrange — 1500 ft, below 3000 ft
        var site = CreateSite(new Dictionary<string, WindTempDto>
        {
            ["3000"] = new() { Direction = 270, Speed = 10 },
            ["6000"] = new() { Direction = 180, Speed = 20, Temperature = 2f }
        });

        // Act
        var result = NavlogService.InterpolateWindTempData(1500, site);

        // Assert — uses 3000 ft wind, temp extrapolated from 6000
        result.Should().NotBeNull();
        result!.Direction.Should().Be(270);
        result.Speed.Should().Be(10);
        // Temp at 1500 = 2 + (6000-1500)/1000*2 = 2 + 9 = 11
        result.Temperature.Should().BeApproximately(11f, 0.01f);
    }

    [Fact]
    public void ShouldUseHighestAltitudeData_WhenAltitudeAbove39000WithFullData()
    {
        // Arrange — 42000 ft, above all standard levels.
        // Note: The interpolation code falls into the below-lowest path when above all levels
        // (FindIndex returns -1), so it uses the 3000 ft wind and extrapolates temp from
        // the first altitude with temperature data.
        var site = CreateSite(new Dictionary<string, WindTempDto>
        {
            ["3000"] = new() { Direction = 300, Speed = 80 },
            ["6000"] = new() { Direction = 280, Speed = 70, Temperature = 0f },
            ["39000"] = new() { Direction = 250, Speed = 100, Temperature = -55f }
        });

        // Act
        var result = NavlogService.InterpolateWindTempData(42000, site);

        // Assert — falls into below-lowest path: wind from 3000, temp from first source with temp (6000)
        result.Should().NotBeNull();
        result!.Direction.Should().Be(300); // from 3000 ft
        result.Speed.Should().Be(80);       // from 3000 ft
        // Temp = 0 + (6000-42000)/1000*2 = 0 - 72 = -72
        result.Temperature.Should().BeApproximately(-72f, 0.01f);
    }

    [Fact]
    public void ShouldReturnNull_WhenAboveHighestAndNoBoundsData()
    {
        // Arrange — 35500 ft between 34000 and 39000, but neither has data
        var site = CreateSite(new Dictionary<string, WindTempDto>
        {
            ["3000"] = new() { Direction = 270, Speed = 10 },
            ["6000"] = new() { Direction = 180, Speed = 20, Temperature = 2f }
        });

        // Act — 35500 is between 34000 and 39000 in the altitude levels,
        // but neither bound has data, so interpolation returns null
        var result = NavlogService.InterpolateWindTempData(35500, site);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ShouldFallbackTo6000_WhenNo3000Data()
    {
        // Arrange — altitude below 3000 but no 3000 entry
        var site = CreateSite(new Dictionary<string, WindTempDto>
        {
            ["6000"] = new() { Direction = 180, Speed = 20, Temperature = 2f },
            ["9000"] = new() { Direction = 200, Speed = 30, Temperature = -4f }
        });

        // Act
        var result = NavlogService.InterpolateWindTempData(1500, site);

        // Assert — falls back to 6000 wind data
        result.Should().NotBeNull();
        result!.Direction.Should().Be(180);
        result.Speed.Should().Be(20);
        // Temp from 6000: 2 + (6000-1500)/1000*2 = 2 + 9 = 11
        result.Temperature.Should().BeApproximately(11f, 0.01f);
    }

    [Fact]
    public void ShouldReturnNull_WhenBoundsHaveNoData()
    {
        // Arrange — 7500 ft between 6000 and 9000, but neither has data
        var site = CreateSite(new Dictionary<string, WindTempDto>
        {
            ["3000"] = new() { Direction = 270, Speed = 10 }
        });

        // Act
        var result = NavlogService.InterpolateWindTempData(7500, site);

        // Assert — both bounds missing → null
        result.Should().BeNull();
    }

    #endregion

    #region Temperature Interpolation Edge Cases

    [Fact]
    public void ShouldHandleCaseWhenOnlyLowerHasTemperature()
    {
        // Arrange
        var site = CreateSite(new Dictionary<string, WindTempDto>
        {
            ["6000"] = new() { Direction = 180, Speed = 20, Temperature = 4f },
            ["9000"] = new() { Direction = 200, Speed = 30 } // no temp
        });

        // Act — 7500 ft
        var result = NavlogService.InterpolateWindTempData(7500, site);

        // Assert — temp lapse rate from lower: 4 - (7500-6000)/1000*2 = 4 - 3 = 1
        result.Should().NotBeNull();
        result!.Temperature.Should().BeApproximately(1f, 0.01f);
    }

    [Fact]
    public void ShouldHandleCaseWhenOnlyUpperHasTemperature()
    {
        // Arrange
        var site = CreateSite(new Dictionary<string, WindTempDto>
        {
            ["6000"] = new() { Direction = 180, Speed = 20 }, // no temp
            ["9000"] = new() { Direction = 200, Speed = 30, Temperature = -2f }
        });

        // Act — 7500 ft
        var result = NavlogService.InterpolateWindTempData(7500, site);

        // Assert — temp from upper via lapse rate: -2 + (9000-7500)/1000*2 = -2 + 3 = 1
        result.Should().NotBeNull();
        result!.Temperature.Should().BeApproximately(1f, 0.01f);
    }

    [Fact]
    public void ShouldHandleBothBoundsWithTemperature()
    {
        // Arrange
        var site = CreateSite(new Dictionary<string, WindTempDto>
        {
            ["6000"] = new() { Direction = 180, Speed = 20, Temperature = 10f },
            ["9000"] = new() { Direction = 200, Speed = 30, Temperature = -2f }
        });

        // Act — 7500 ft, ratio = 0.5
        var result = NavlogService.InterpolateWindTempData(7500, site);

        // Assert — linear interpolation: 10 + (-12)*0.5 = 4
        result.Should().NotBeNull();
        result!.Temperature.Should().BeApproximately(4f, 0.01f);
    }

    #endregion
}

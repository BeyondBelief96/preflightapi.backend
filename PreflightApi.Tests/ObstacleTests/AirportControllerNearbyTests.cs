using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using PreflightApi.API.Controllers;
using PreflightApi.API.Models;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;
using Xunit;

namespace PreflightApi.Tests.ObstacleTests;

public class AirportControllerNearbyTests
{
    private readonly IAirportService _airportService;
    private readonly AirportController _sut;

    public AirportControllerNearbyTests()
    {
        _airportService = Substitute.For<IAirportService>();
        _sut = new AirportController(_airportService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task SearchNearby_HappyPath_ReturnsAirports()
    {
        var expected = new PaginatedResponse<AirportDto>
        {
            Data = new List<AirportDto> { new() { ArptId = "DFW" } },
            Pagination = new PaginationMetadata { Limit = 100, HasMore = false }
        };
        _airportService.SearchNearby(32.89m, -97.03m, 30, null, 100).Returns(expected);

        var result = await _sut.SearchNearby(32.89m, -97.03m, new PaginationParams(), CancellationToken.None);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expected);
    }

    [Fact]
    public async Task SearchNearby_CustomRadius_PassedToService()
    {
        var expected = new PaginatedResponse<AirportDto>
        {
            Data = new List<AirportDto>(),
            Pagination = new PaginationMetadata { Limit = 100, HasMore = false }
        };
        _airportService.SearchNearby(32.89m, -97.03m, 50, null, 100).Returns(expected);

        await _sut.SearchNearby(32.89m, -97.03m, new PaginationParams(), CancellationToken.None, radiusNm: 50);

        await _airportService.Received(1).SearchNearby(32.89m, -97.03m, 50, Arg.Any<string?>(), Arg.Any<int>());
    }

    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    public async Task SearchNearby_InvalidLatitude_ThrowsValidationException(decimal lat)
    {
        var act = () => _sut.SearchNearby(lat, -97.03m, new PaginationParams(), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Latitude must be between -90 and 90*");
    }

    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    public async Task SearchNearby_InvalidLongitude_ThrowsValidationException(decimal lon)
    {
        var act = () => _sut.SearchNearby(32.89m, lon, new PaginationParams(), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Longitude must be between -180 and 180*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task SearchNearby_InvalidRadius_ThrowsValidationException(double radius)
    {
        var act = () => _sut.SearchNearby(32.89m, -97.03m, new PaginationParams(), CancellationToken.None, radiusNm: radius);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Radius must be greater than 0*");
    }

    [Fact]
    public async Task SearchNearby_DefaultRadius_Is30Nm()
    {
        var expected = new PaginatedResponse<AirportDto>
        {
            Data = new List<AirportDto>(),
            Pagination = new PaginationMetadata { Limit = 100, HasMore = false }
        };
        _airportService.SearchNearby(32.89m, -97.03m, 30, null, 100).Returns(expected);

        await _sut.SearchNearby(32.89m, -97.03m, new PaginationParams(), CancellationToken.None);

        await _airportService.Received(1).SearchNearby(32.89m, -97.03m, 30, Arg.Any<string?>(), Arg.Any<int>());
    }
}

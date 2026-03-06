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

public class ObstacleControllerTests
{
    private readonly IObstacleService _obstacleService;
    private readonly IAirportService _airportService;
    private readonly ObstacleController _sut;

    public ObstacleControllerTests()
    {
        _obstacleService = Substitute.For<IObstacleService>();
        _airportService = Substitute.For<IAirportService>();
        _sut = new ObstacleController(_obstacleService, _airportService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task SearchNearAirport_HappyPath_ReturnsObstacles()
    {
        var airport = new AirportDto { IcaoId = "KDFW", LatDecimal = 32.8968m, LongDecimal = -97.0380m };
        _airportService.GetAirportByIcaoCodeOrIdent("KDFW").Returns(airport);

        var expected = new PaginatedResponse<ObstacleDto>
        {
            Data = new List<ObstacleDto> { new() { OasNumber = "12-345678" } },
            Pagination = new PaginationMetadata { Limit = 100, HasMore = false }
        };
        _obstacleService.SearchNearby(32.8968m, -97.0380m, 10, null, null, 100).Returns(expected);

        var result = await _sut.SearchNearAirport("KDFW", new PaginationParams(), CancellationToken.None);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expected);
    }

    [Fact]
    public async Task SearchNearAirport_AirportNotFound_ThrowsAirportNotFoundException()
    {
        _airportService.GetAirportByIcaoCodeOrIdent("XXXX")
            .Returns<AirportDto>(x => throw new AirportNotFoundException("XXXX"));

        var act = () => _sut.SearchNearAirport("XXXX", new PaginationParams(), CancellationToken.None);

        await act.Should().ThrowAsync<AirportNotFoundException>();
    }

    [Fact]
    public async Task SearchNearAirport_AirportMissingCoordinates_ThrowsValidationException()
    {
        var airport = new AirportDto { IcaoId = "KDFW", LatDecimal = null, LongDecimal = null };
        _airportService.GetAirportByIcaoCodeOrIdent("KDFW").Returns(airport);

        var act = () => _sut.SearchNearAirport("KDFW", new PaginationParams(), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*does not have coordinates*");
    }

    [Fact]
    public async Task SearchNearAirport_DefaultRadius_Uses10Nm()
    {
        var airport = new AirportDto { IcaoId = "KDFW", LatDecimal = 32.8968m, LongDecimal = -97.0380m };
        _airportService.GetAirportByIcaoCodeOrIdent("KDFW").Returns(airport);

        var expected = new PaginatedResponse<ObstacleDto>
        {
            Data = new List<ObstacleDto>(),
            Pagination = new PaginationMetadata { Limit = 100, HasMore = false }
        };
        _obstacleService.SearchNearby(32.8968m, -97.0380m, 10, null, null, 100).Returns(expected);

        await _sut.SearchNearAirport("KDFW", new PaginationParams(), CancellationToken.None);

        await _obstacleService.Received(1).SearchNearby(32.8968m, -97.0380m, 10, Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<int>());
    }

    [Fact]
    public async Task SearchNearAirport_ZeroRadius_ThrowsValidationException()
    {
        var act = () => _sut.SearchNearAirport("KDFW", new PaginationParams(), CancellationToken.None, radiusNm: 0);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Radius must be greater than 0*");
    }

    [Fact]
    public async Task SearchNearAirport_NegativeRadius_ThrowsValidationException()
    {
        var act = () => _sut.SearchNearAirport("KDFW", new PaginationParams(), CancellationToken.None, radiusNm: -5);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Radius must be greater than 0*");
    }
}

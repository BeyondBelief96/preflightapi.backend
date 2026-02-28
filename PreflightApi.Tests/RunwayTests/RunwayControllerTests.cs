using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using PreflightApi.API.Controllers;
using PreflightApi.API.Models;
using PreflightApi.Domain.Enums;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;
using Xunit;

namespace PreflightApi.Tests.RunwayTests;

public class RunwayControllerTests
{
    private readonly IRunwayService _runwayService;
    private readonly RunwayController _sut;

    public RunwayControllerTests()
    {
        _runwayService = Substitute.For<IRunwayService>();
        _sut = new RunwayController(_runwayService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    #region GetRunwaysByAirport Tests

    [Fact]
    public async Task GetRunwaysByAirport_HappyPath_ReturnsRunways()
    {
        var expected = new List<RunwayDto>
        {
            new() { RunwayId = "17L/35R", AirportIcaoCode = "KDFW", AirportArptId = "DFW", AirportName = "DALLAS-FT WORTH INTL" }
        };
        _runwayService.GetRunwaysByAirportAsync("KDFW", false).Returns(expected);

        var result = await _sut.GetRunwaysByAirport("KDFW");

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<List<RunwayDto>>().Subject;
        data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetRunwaysByAirport_WithGeometry_PassesIncludeGeometry()
    {
        _runwayService.GetRunwaysByAirportAsync("KDFW", true).Returns(new List<RunwayDto>());

        await _sut.GetRunwaysByAirport("KDFW", includeGeometry: true);

        await _runwayService.Received(1).GetRunwaysByAirportAsync("KDFW", true);
    }

    [Fact]
    public async Task GetRunwaysByAirport_EmptyIdentifier_ThrowsValidationException()
    {
        var act = () => _sut.GetRunwaysByAirport("");

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*ICAO code or identifier is required*");
    }

    [Fact]
    public async Task GetRunwaysByAirport_WhitespaceIdentifier_ThrowsValidationException()
    {
        var act = () => _sut.GetRunwaysByAirport("   ");

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*ICAO code or identifier is required*");
    }

    #endregion

    #region GetRunways Tests

    [Fact]
    public async Task GetRunways_HappyPath_ReturnsPaginatedRunways()
    {
        var expected = new PaginatedResponse<RunwayDto>
        {
            Data = [new RunwayDto { RunwayId = "17L/35R", AirportArptId = "DFW" }],
            Pagination = new PaginationMetadata { Limit = 100, HasMore = false }
        };
        _runwayService.GetRunways(null, null, null, null, null, null, 100).Returns(expected);

        var result = await _sut.GetRunways();

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expected);
    }

    [Fact]
    public async Task GetRunways_WithFilters_PassesParametersToService()
    {
        var expected = PaginatedResponse<RunwayDto>.Empty(50);
        _runwayService.GetRunways("DFW", RunwaySurfaceType.Asphalt, 5000, "TX", true, null, 50).Returns(expected);

        var pagination = new PaginationParams { Limit = 50 };
        await _sut.GetRunways(search: "DFW", surfaceType: RunwaySurfaceType.Asphalt, minLength: 5000, state: "TX", lighted: true, pagination: pagination);

        await _runwayService.Received(1).GetRunways("DFW", RunwaySurfaceType.Asphalt, 5000, "TX", true, null, 50);
    }

    [Fact]
    public async Task GetRunways_ClampsLimitToMax500()
    {
        var expected = PaginatedResponse<RunwayDto>.Empty(500);
        _runwayService.GetRunways(null, null, null, null, null, null, 500).Returns(expected);

        var pagination = new PaginationParams { Limit = 999 };
        await _sut.GetRunways(pagination: pagination);

        await _runwayService.Received(1).GetRunways(null, null, null, null, null, null, 500);
    }

    [Fact]
    public async Task GetRunways_ClampsLimitToMin1()
    {
        var expected = PaginatedResponse<RunwayDto>.Empty(1);
        _runwayService.GetRunways(null, null, null, null, null, null, 1).Returns(expected);

        var pagination = new PaginationParams { Limit = 0 };
        await _sut.GetRunways(pagination: pagination);

        await _runwayService.Received(1).GetRunways(null, null, null, null, null, null, 1);
    }

    #endregion

    #region SearchNearby Tests

    [Fact]
    public async Task SearchNearby_HappyPath_ReturnsRunways()
    {
        var expected = new PaginatedResponse<RunwayDto>
        {
            Data = [new RunwayDto { RunwayId = "17L/35R", AirportArptId = "DFW" }],
            Pagination = new PaginationMetadata { Limit = 100, HasMore = false }
        };
        _runwayService.SearchNearby(32.897m, -97.038m, 30, null, null, false, null, 100).Returns(expected);

        var result = await _sut.SearchNearby(32.897m, -97.038m);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expected);
    }

    [Fact]
    public async Task SearchNearby_WithFilters_PassesToService()
    {
        var expected = PaginatedResponse<RunwayDto>.Empty(100);
        _runwayService.SearchNearby(32.897m, -97.038m, 50, 4000, RunwaySurfaceType.Concrete, true, null, 100)
            .Returns(expected);

        await _sut.SearchNearby(32.897m, -97.038m, 50, minLength: 4000, surfaceType: RunwaySurfaceType.Concrete, includeGeometry: true);

        await _runwayService.Received(1).SearchNearby(32.897m, -97.038m, 50, 4000, RunwaySurfaceType.Concrete, true, null, 100);
    }

    [Fact]
    public async Task SearchNearby_InvalidLatitude_ThrowsValidationException()
    {
        var act = () => _sut.SearchNearby(91m, -97.038m);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Latitude*");
    }

    [Fact]
    public async Task SearchNearby_InvalidLongitude_ThrowsValidationException()
    {
        var act = () => _sut.SearchNearby(32.897m, -181m);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Longitude*");
    }

    [Fact]
    public async Task SearchNearby_ZeroRadius_ThrowsValidationException()
    {
        var act = () => _sut.SearchNearby(32.897m, -97.038m, radiusNm: 0);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Radius must be greater than 0*");
    }

    [Fact]
    public async Task SearchNearby_NegativeRadius_ThrowsValidationException()
    {
        var act = () => _sut.SearchNearby(32.897m, -97.038m, radiusNm: -5);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Radius must be greater than 0*");
    }

    [Fact]
    public async Task SearchNearby_ExcessiveRadius_ThrowsValidationException()
    {
        var act = () => _sut.SearchNearby(32.897m, -97.038m, radiusNm: 501);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*cannot exceed 500*");
    }

    [Fact]
    public async Task SearchNearby_DefaultRadius_Uses30Nm()
    {
        var expected = PaginatedResponse<RunwayDto>.Empty(100);
        _runwayService.SearchNearby(32.897m, -97.038m, 30, null, null, false, null, 100).Returns(expected);

        await _sut.SearchNearby(32.897m, -97.038m);

        await _runwayService.Received(1).SearchNearby(32.897m, -97.038m, 30, Arg.Any<int?>(), Arg.Any<RunwaySurfaceType?>(), Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<int>());
    }

    [Fact]
    public async Task SearchNearby_ClampsLimitToMax500()
    {
        var expected = PaginatedResponse<RunwayDto>.Empty(500);
        _runwayService.SearchNearby(32.897m, -97.038m, 30, null, null, false, null, 500).Returns(expected);

        var pagination = new PaginationParams { Limit = 999 };
        await _sut.SearchNearby(32.897m, -97.038m, pagination: pagination);

        await _runwayService.Received(1).SearchNearby(32.897m, -97.038m, 30, null, null, false, null, 500);
    }

    #endregion
}

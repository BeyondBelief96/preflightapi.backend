using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using PreflightApi.API.Controllers;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.API.Models;
using PreflightApi.Infrastructure.Interfaces;
using Xunit;

namespace PreflightApi.Tests.WeatherServicesTests;

public class PirepControllerTests
{
    private readonly IPirepService _pirepService;
    private readonly IAirportService _airportService;
    private readonly PirepController _sut;

    public PirepControllerTests()
    {
        _pirepService = Substitute.For<IPirepService>();
        _airportService = Substitute.For<IAirportService>();
        _sut = new PirepController(_pirepService, _airportService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task SearchNearby_HappyPath_ReturnsPireps()
    {
        var expected = new PaginatedResponse<PirepDto>
        {
            Data = new List<PirepDto> { new() { Id = 1, RawText = "UA /OV DFW" } },
            Pagination = new PaginationMetadata { Limit = 100, HasMore = false }
        };
        _pirepService.SearchNearby(32.8968, -97.0380, 50, null, 100, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.SearchNearby(32.8968, -97.0380, new PaginationParams { Limit = 100 }, CancellationToken.None, 50);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expected);
    }

    [Fact]
    public async Task SearchNearby_ZeroRadius_ThrowsValidationException()
    {
        var act = () => _sut.SearchNearby(32.8968, -97.0380, new PaginationParams(), CancellationToken.None, radiusNm: 0);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Radius must be greater than 0*");
    }

    [Fact]
    public async Task SearchNearby_NegativeRadius_ThrowsValidationException()
    {
        var act = () => _sut.SearchNearby(32.8968, -97.0380, new PaginationParams(), CancellationToken.None, radiusNm: -5);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Radius must be greater than 0*");
    }

    [Fact]
    public async Task SearchNearby_LatitudeTooHigh_ThrowsValidationException()
    {
        var act = () => _sut.SearchNearby(91, -97.0380, new PaginationParams(), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Latitude must be between -90 and 90*");
    }

    [Fact]
    public async Task SearchNearby_LatitudeTooLow_ThrowsValidationException()
    {
        var act = () => _sut.SearchNearby(-91, -97.0380, new PaginationParams(), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Latitude must be between -90 and 90*");
    }

    [Fact]
    public async Task SearchNearby_LongitudeTooHigh_ThrowsValidationException()
    {
        var act = () => _sut.SearchNearby(32.8968, 181, new PaginationParams(), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Longitude must be between -180 and 180*");
    }

    [Fact]
    public async Task SearchNearby_LongitudeTooLow_ThrowsValidationException()
    {
        var act = () => _sut.SearchNearby(32.8968, -181, new PaginationParams(), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Longitude must be between -180 and 180*");
    }

    [Fact]
    public async Task SearchNearAirport_HappyPath_ReturnsPireps()
    {
        var airport = new AirportDto { IcaoId = "KDFW", LatDecimal = 32.8968, LongDecimal = -97.0380 };
        _airportService.GetAirportByIcaoCodeOrIdent("KDFW").Returns(airport);

        var expected = new PaginatedResponse<PirepDto>
        {
            Data = new List<PirepDto> { new() { Id = 1, RawText = "UA /OV DFW" } },
            Pagination = new PaginationMetadata { Limit = 100, HasMore = false }
        };
        _pirepService.SearchNearby(32.8968, -97.0380, 50, null, 100, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.SearchNearAirport("KDFW", new PaginationParams { Limit = 100 }, CancellationToken.None, 50);

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
    public async Task SearchNearAirport_DefaultRadius_Uses50Nm()
    {
        var airport = new AirportDto { IcaoId = "KDFW", LatDecimal = 32.8968, LongDecimal = -97.0380 };
        _airportService.GetAirportByIcaoCodeOrIdent("KDFW").Returns(airport);

        var expected = new PaginatedResponse<PirepDto>
        {
            Data = new List<PirepDto>(),
            Pagination = new PaginationMetadata { Limit = 100, HasMore = false }
        };
        _pirepService.SearchNearby(32.8968, -97.0380, 50, null, 100, Arg.Any<CancellationToken>()).Returns(expected);

        await _sut.SearchNearAirport("KDFW", new PaginationParams(), CancellationToken.None);

        await _pirepService.Received(1).SearchNearby(32.8968, -97.0380, 50, Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
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

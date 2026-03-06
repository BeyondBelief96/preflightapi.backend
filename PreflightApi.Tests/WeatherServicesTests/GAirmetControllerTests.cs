using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using PreflightApi.API.Controllers;
using PreflightApi.API.Models;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;
using Xunit;

namespace PreflightApi.Tests.WeatherServicesTests;

public class GAirmetControllerTests
{
    private readonly IGAirmetService _gairmetService;
    private readonly GAirmetController _sut;

    public GAirmetControllerTests()
    {
        _gairmetService = Substitute.For<IGAirmetService>();
        _sut = new GAirmetController(_gairmetService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task SearchAffecting_HappyPath_ReturnsGAirmets()
    {
        var expected = new PaginatedResponse<GAirmetDto>
        {
            Data = new List<GAirmetDto> { new() { Id = 1, Hazard = GAirmetHazardType.ICE } },
            Pagination = new PaginationMetadata { Limit = 100, HasMore = false }
        };
        _gairmetService.SearchAffecting(32.8968m, -97.0380m, null, 100, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.SearchAffecting(32.8968m, -97.0380m, new PaginationParams { Limit = 100 }, CancellationToken.None);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expected);
    }

    [Fact]
    public async Task SearchAffecting_LatitudeTooHigh_ThrowsValidationException()
    {
        var act = () => _sut.SearchAffecting(91m, -97.0380m, new PaginationParams(), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Latitude must be between -90 and 90*");
    }

    [Fact]
    public async Task SearchAffecting_LatitudeTooLow_ThrowsValidationException()
    {
        var act = () => _sut.SearchAffecting(-91m, -97.0380m, new PaginationParams(), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Latitude must be between -90 and 90*");
    }

    [Fact]
    public async Task SearchAffecting_LongitudeTooHigh_ThrowsValidationException()
    {
        var act = () => _sut.SearchAffecting(32.8968m, 181m, new PaginationParams(), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Longitude must be between -180 and 180*");
    }

    [Fact]
    public async Task SearchAffecting_LongitudeTooLow_ThrowsValidationException()
    {
        var act = () => _sut.SearchAffecting(32.8968m, -181m, new PaginationParams(), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Longitude must be between -180 and 180*");
    }
}

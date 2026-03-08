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

namespace PreflightApi.Tests.WeatherServicesTests;

public class SigmetControllerTests
{
    private readonly ISigmetService _sigmetService;
    private readonly SigmetController _sut;

    public SigmetControllerTests()
    {
        _sigmetService = Substitute.For<ISigmetService>();
        _sut = new SigmetController(_sigmetService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    // ── SearchAffecting ──

    [Fact]
    public async Task SearchAffecting_HappyPath_ReturnsSigmets()
    {
        var expected = new PaginatedResponse<SigmetDto>
        {
            Data = new List<SigmetDto> { new() { Id = 1, RawText = "SIGMET CONVECTIVE" } },
            Pagination = new PaginationMetadata { Limit = 100, HasMore = false }
        };
        _sigmetService.SearchAffecting(32.8968, -97.0380, null, 100, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.SearchAffecting(32.8968, -97.0380, new PaginationParams { Limit = 100 }, CancellationToken.None);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expected);
    }

    [Fact]
    public async Task SearchAffecting_LatitudeTooHigh_ThrowsValidationException()
    {
        var act = () => _sut.SearchAffecting(91, -97.0380, new PaginationParams(), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Latitude must be between -90 and 90*");
    }

    [Fact]
    public async Task SearchAffecting_LatitudeTooLow_ThrowsValidationException()
    {
        var act = () => _sut.SearchAffecting(-91, -97.0380, new PaginationParams(), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Latitude must be between -90 and 90*");
    }

    [Fact]
    public async Task SearchAffecting_LongitudeTooHigh_ThrowsValidationException()
    {
        var act = () => _sut.SearchAffecting(32.8968, 181, new PaginationParams(), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Longitude must be between -180 and 180*");
    }

    [Fact]
    public async Task SearchAffecting_LongitudeTooLow_ThrowsValidationException()
    {
        var act = () => _sut.SearchAffecting(32.8968, -181, new PaginationParams(), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Longitude must be between -180 and 180*");
    }

    // ── SearchByArea ──

    [Fact]
    public async Task SearchByArea_HappyPath_ReturnsSigmets()
    {
        var expected = new PaginatedResponse<SigmetDto>
        {
            Data = new List<SigmetDto> { new() { Id = 1, RawText = "SIGMET CONVECTIVE" } },
            Pagination = new PaginationMetadata { Limit = 100, HasMore = false }
        };
        _sigmetService.SearchByArea(30, 35, -100, -95, null, 100, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.SearchByArea(30, 35, -100, -95, new PaginationParams { Limit = 100 }, CancellationToken.None);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expected);
    }

    [Fact]
    public async Task SearchByArea_MinLatGreaterThanMaxLat_ThrowsValidationException()
    {
        var act = () => _sut.SearchByArea(35, 30, -100, -95, new PaginationParams(), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*minLat must be less than maxLat*");
    }

    [Fact]
    public async Task SearchByArea_MinLonGreaterThanMaxLon_ThrowsValidationException()
    {
        var act = () => _sut.SearchByArea(30, 35, -95, -100, new PaginationParams(), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*minLon must be less than maxLon*");
    }

    [Fact]
    public async Task SearchByArea_InvalidLatitude_ThrowsValidationException()
    {
        var act = () => _sut.SearchByArea(-91, 35, -100, -95, new PaginationParams(), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Latitude values must be between -90 and 90*");
    }

    [Fact]
    public async Task SearchByArea_InvalidLongitude_ThrowsValidationException()
    {
        var act = () => _sut.SearchByArea(30, 35, -181, -95, new PaginationParams(), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Longitude values must be between -180 and 180*");
    }
}

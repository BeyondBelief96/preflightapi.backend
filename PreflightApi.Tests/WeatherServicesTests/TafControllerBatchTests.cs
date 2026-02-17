using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using PreflightApi.API.Controllers;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;
using Xunit;

namespace PreflightApi.Tests.WeatherServicesTests;

public class TafControllerBatchTests
{
    private readonly ITafService _tafService;
    private readonly TafController _sut;

    public TafControllerBatchTests()
    {
        _tafService = Substitute.For<ITafService>();
        _sut = new TafController(_tafService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task GetTafsBatch_EmptyIds_ThrowsValidationException()
    {
        var act = () => _sut.GetTafsBatch("");

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task GetTafsBatch_NullIds_ThrowsValidationException()
    {
        var act = () => _sut.GetTafsBatch(null!);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task GetTafsBatch_ValidIds_ReturnsTafs()
    {
        var expected = new List<TafDto>
        {
            new() { StationId = "KDFW", RawText = "TAF KDFW 011720Z 0118/0224 18010KT P6SM SKC" },
            new() { StationId = "KAUS", RawText = "TAF KAUS 011720Z 0118/0224 17008KT P6SM FEW250" }
        };
        _tafService.GetTafsForAirports(Arg.Any<string[]>()).Returns(expected);

        var result = await _sut.GetTafsBatch("KDFW,KAUS");

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expected);
        await _tafService.Received(1).GetTafsForAirports(
            Arg.Is<string[]>(a => a.Length == 2 && a[0] == "KDFW" && a[1] == "KAUS"));
    }

    [Fact]
    public async Task GetTafsBatch_TrimsWhitespace_PassesCleanArray()
    {
        _tafService.GetTafsForAirports(Arg.Any<string[]>()).Returns(new List<TafDto>());

        await _sut.GetTafsBatch(" KDFW , KAUS , ");

        await _tafService.Received(1).GetTafsForAirports(
            Arg.Is<string[]>(a => a.Length == 2 && a[0] == "KDFW" && a[1] == "KAUS"));
    }
}

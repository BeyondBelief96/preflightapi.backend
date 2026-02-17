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

public class MetarControllerBatchTests
{
    private readonly IMetarService _metarService;
    private readonly MetarController _sut;

    public MetarControllerBatchTests()
    {
        _metarService = Substitute.For<IMetarService>();
        _sut = new MetarController(_metarService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task GetMetarsBatch_EmptyIds_ThrowsValidationException()
    {
        var act = () => _sut.GetMetarsBatch("");

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task GetMetarsBatch_NullIds_ThrowsValidationException()
    {
        var act = () => _sut.GetMetarsBatch(null!);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task GetMetarsBatch_ValidIds_ReturnsMetars()
    {
        var expected = new List<MetarDto>
        {
            new() { StationId = "KDFW", RawText = "KDFW 011953Z 18010KT 10SM CLR 30/15 A2990" },
            new() { StationId = "KAUS", RawText = "KAUS 011953Z 17008KT 10SM FEW250 31/16 A2988" }
        };
        _metarService.GetMetarsForAirports(Arg.Any<string[]>()).Returns(expected);

        var result = await _sut.GetMetarsBatch("KDFW,KAUS");

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expected);
        await _metarService.Received(1).GetMetarsForAirports(
            Arg.Is<string[]>(a => a.Length == 2 && a[0] == "KDFW" && a[1] == "KAUS"));
    }

    [Fact]
    public async Task GetMetarsBatch_TrimsWhitespace_PassesCleanArray()
    {
        _metarService.GetMetarsForAirports(Arg.Any<string[]>()).Returns(new List<MetarDto>());

        await _sut.GetMetarsBatch(" KDFW , KAUS , ");

        await _metarService.Received(1).GetMetarsForAirports(
            Arg.Is<string[]>(a => a.Length == 2 && a[0] == "KDFW" && a[1] == "KAUS"));
    }
}

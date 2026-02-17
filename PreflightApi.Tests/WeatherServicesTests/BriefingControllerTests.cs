using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using PreflightApi.API.Controllers;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Dtos.Briefing;
using PreflightApi.Infrastructure.Interfaces;
using Xunit;

namespace PreflightApi.Tests.WeatherServicesTests;

public class BriefingControllerTests
{
    private readonly IBriefingService _briefingService;
    private readonly BriefingController _sut;

    public BriefingControllerTests()
    {
        _briefingService = Substitute.For<IBriefingService>();
        _sut = new BriefingController(_briefingService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task GetRouteBriefing_HappyPath_ReturnsBriefing()
    {
        var request = new RouteBriefingRequest
        {
            Waypoints = new List<BriefingWaypoint>
            {
                new() { AirportIdentifier = "KDFW" },
                new() { AirportIdentifier = "KAUS" }
            },
            CorridorWidthNm = 25
        };

        var expected = new RouteBriefingResponse
        {
            Route = "KDFW -> KAUS",
            CorridorWidthNm = 25,
            Summary = new RouteBriefingSummary { MetarCount = 2, TafCount = 2 }
        };
        _briefingService.GetRouteBriefingAsync(request, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.GetRouteBriefing(request, CancellationToken.None);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expected);
    }

    [Fact]
    public async Task GetRouteBriefing_ServiceThrowsValidation_PropagatesException()
    {
        var request = new RouteBriefingRequest
        {
            Waypoints = new List<BriefingWaypoint>
            {
                new() { AirportIdentifier = "KDFW" }
            }
        };
        _briefingService.GetRouteBriefingAsync(request, Arg.Any<CancellationToken>())
            .Returns<RouteBriefingResponse>(x => throw new ValidationException("waypoints", "At least two waypoints are required"));

        var act = () => _sut.GetRouteBriefing(request, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*At least two waypoints*");
    }

    [Fact]
    public async Task GetRouteBriefing_AirportNotFound_PropagatesException()
    {
        var request = new RouteBriefingRequest
        {
            Waypoints = new List<BriefingWaypoint>
            {
                new() { AirportIdentifier = "XXXX" },
                new() { AirportIdentifier = "KAUS" }
            }
        };
        _briefingService.GetRouteBriefingAsync(request, Arg.Any<CancellationToken>())
            .Returns<RouteBriefingResponse>(x => throw new AirportNotFoundException("XXXX"));

        var act = () => _sut.GetRouteBriefing(request, CancellationToken.None);

        await act.Should().ThrowAsync<AirportNotFoundException>();
    }
}

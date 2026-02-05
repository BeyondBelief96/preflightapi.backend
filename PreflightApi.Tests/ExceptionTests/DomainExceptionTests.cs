using FluentAssertions;
using PreflightApi.Domain.Exceptions;
using Xunit;

namespace PreflightApi.Tests.ExceptionTests;

public class DomainExceptionTests
{
    [Fact]
    public void ObstacleNotFoundException_ShouldSetCorrectErrorCodeAndMessage()
    {
        var oasNumber = "08-000001";

        var exception = new ObstacleNotFoundException(oasNumber);

        exception.ErrorCode.Should().Be(ErrorCodes.ObstacleNotFound);
        exception.UserMessage.Should().Contain(oasNumber);
    }

    [Fact]
    public void RunwayNotFoundException_ShouldSetCorrectErrorCodeAndMessage()
    {
        var airportId = "KDFW";

        var exception = new RunwayNotFoundException(airportId);

        exception.ErrorCode.Should().Be(ErrorCodes.RunwayNotFound);
        exception.UserMessage.Should().Contain(airportId);
    }

    [Fact]
    public void ObstacleNotFoundException_ShouldBeNotFoundExceptionSubclass()
    {
        var exception = new ObstacleNotFoundException("08-000001");

        exception.Should().BeAssignableTo<NotFoundException>();
        exception.Should().BeAssignableTo<DomainException>();
    }

    [Fact]
    public void RunwayNotFoundException_ShouldBeNotFoundExceptionSubclass()
    {
        var exception = new RunwayNotFoundException("KDFW");

        exception.Should().BeAssignableTo<NotFoundException>();
        exception.Should().BeAssignableTo<DomainException>();
    }

    [Fact]
    public void ErrorCodes_ShouldContainObstacleNotFound()
    {
        ErrorCodes.ObstacleNotFound.Should().Be("OBSTACLE_NOT_FOUND");
    }

    [Fact]
    public void ErrorCodes_ShouldContainRunwayNotFound()
    {
        ErrorCodes.RunwayNotFound.Should().Be("RUNWAY_NOT_FOUND");
    }

    [Fact]
    public void ErrorCodes_ShouldContainCommunicationFrequencyNotFound()
    {
        ErrorCodes.CommunicationFrequencyNotFound.Should().Be("COMMUNICATION_FREQUENCY_NOT_FOUND");
    }
}

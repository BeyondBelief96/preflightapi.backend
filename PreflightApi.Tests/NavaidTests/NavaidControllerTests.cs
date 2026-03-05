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

namespace PreflightApi.Tests.NavaidTests;

public class NavaidControllerTests
{
    private readonly INavaidService _navaidService;
    private readonly NavaidController _sut;

    public NavaidControllerTests()
    {
        _navaidService = Substitute.For<INavaidService>();
        _sut = new NavaidController(_navaidService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    #region GetNavaids Tests

    [Fact]
    public async Task GetNavaids_HappyPath_ReturnsPaginatedNavaids()
    {
        var expected = new PaginatedResponse<NavaidDto>
        {
            Data = [new NavaidDto { NavId = "DFW", NavType = NavaidType.Vortac }],
            Pagination = new PaginationMetadata { Limit = 100, HasMore = false }
        };
        _navaidService.GetNavaids(null, null, null, null, 100).Returns(expected);

        var result = await _sut.GetNavaids();

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expected);
    }

    [Fact]
    public async Task GetNavaids_WithFilters_PassesParametersToService()
    {
        var expected = new PaginatedResponse<NavaidDto>
        {
            Data = [],
            Pagination = new PaginationMetadata { Limit = 50, HasMore = false }
        };
        _navaidService.GetNavaids("DFW", "VOR", "TX", null, 50).Returns(expected);

        var pagination = new PaginationParams { Limit = 50 };
        await _sut.GetNavaids(search: "DFW", type: "VOR", state: "TX", pagination: pagination);

        await _navaidService.Received(1).GetNavaids("DFW", "VOR", "TX", null, 50);
    }

    [Fact]
    public async Task GetNavaids_ClampsLimitToMax500()
    {
        var expected = new PaginatedResponse<NavaidDto>
        {
            Data = [],
            Pagination = new PaginationMetadata { Limit = 500, HasMore = false }
        };
        _navaidService.GetNavaids(null, null, null, null, 500).Returns(expected);

        var pagination = new PaginationParams { Limit = 999 };
        await _sut.GetNavaids(pagination: pagination);

        await _navaidService.Received(1).GetNavaids(null, null, null, null, 500);
    }

    #endregion

    #region GetByType Tests

    [Fact]
    public async Task GetByType_HappyPath_ReturnsPaginatedNavaids()
    {
        var expected = new PaginatedResponse<NavaidDto>
        {
            Data = [new NavaidDto { NavId = "DFW", NavType = NavaidType.Vor }],
            Pagination = new PaginationMetadata { Limit = 100, HasMore = false }
        };
        _navaidService.GetNavaids(null, "VOR", null, null, 100).Returns(expected);

        var result = await _sut.GetByType(NavaidType.Vor);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expected);
    }

    [Fact]
    public async Task GetByType_WithPagination_PassesCursorAndLimit()
    {
        var expected = new PaginatedResponse<NavaidDto>
        {
            Data = [],
            Pagination = new PaginationMetadata { Limit = 25, HasMore = true, NextCursor = "abc" }
        };
        _navaidService.GetNavaids(null, "VORTAC", null, "cursor123", 25).Returns(expected);

        var pagination = new PaginationParams { Limit = 25, Cursor = "cursor123" };
        var result = await _sut.GetByType(NavaidType.Vortac, pagination);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expected);
    }

    [Fact]
    public async Task GetByType_VorDme_PassesSlashedDbString()
    {
        var expected = new PaginatedResponse<NavaidDto>
        {
            Data = [],
            Pagination = new PaginationMetadata { Limit = 100, HasMore = false }
        };
        _navaidService.GetNavaids(null, "VOR/DME", null, null, 100).Returns(expected);

        await _sut.GetByType(NavaidType.VorDme);

        await _navaidService.Received(1).GetNavaids(null, "VOR/DME", null, null, 100);
    }

    [Fact]
    public async Task GetByType_ClampsLimitToMax500()
    {
        var expected = new PaginatedResponse<NavaidDto>
        {
            Data = [],
            Pagination = new PaginationMetadata { Limit = 500, HasMore = false }
        };
        _navaidService.GetNavaids(null, "NDB", null, null, 500).Returns(expected);

        var pagination = new PaginationParams { Limit = 999 };
        await _sut.GetByType(NavaidType.Ndb, pagination);

        await _navaidService.Received(1).GetNavaids(null, "NDB", null, null, 500);
    }

    #endregion

    #region GetByIdentifier Tests

    [Fact]
    public async Task GetByIdentifier_HappyPath_ReturnsNavaids()
    {
        var navaids = new List<NavaidDto>
        {
            new() { NavId = "DFW", NavType = NavaidType.Vortac }
        };
        _navaidService.GetNavaidsByIdentifier("DFW", Arg.Any<CancellationToken>()).Returns(navaids);

        var result = await _sut.GetByIdentifier("DFW", CancellationToken.None);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<List<NavaidDto>>().Subject;
        data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdentifier_EmptyNavId_ThrowsValidationException()
    {
        var act = () => _sut.GetByIdentifier("", CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*NAVAID identifier is required*");
    }

    [Fact]
    public async Task GetByIdentifier_WhitespaceNavId_ThrowsValidationException()
    {
        var act = () => _sut.GetByIdentifier("   ", CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*NAVAID identifier is required*");
    }

    [Fact]
    public async Task GetByIdentifier_NotFound_ThrowsNavaidNotFoundException()
    {
        _navaidService.GetNavaidsByIdentifier("ZZZ", Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<NavaidDto>());

        var act = () => _sut.GetByIdentifier("ZZZ", CancellationToken.None);

        await act.Should().ThrowAsync<NavaidNotFoundException>()
            .WithMessage("*No navaids were found*ZZZ*");
    }

    [Fact]
    public async Task GetByIdentifier_ReturnsMultiple_WhenSameNavId()
    {
        var navaids = new List<NavaidDto>
        {
            new() { NavId = "TST", NavType = NavaidType.Vor },
            new() { NavId = "TST", NavType = NavaidType.Ndb }
        };
        _navaidService.GetNavaidsByIdentifier("TST", Arg.Any<CancellationToken>()).Returns(navaids);

        var result = await _sut.GetByIdentifier("TST", CancellationToken.None);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<List<NavaidDto>>().Subject;
        data.Should().HaveCount(2);
    }

    #endregion

    #region GetBatch Tests

    [Fact]
    public async Task GetBatch_HappyPath_ReturnsNavaids()
    {
        var navaids = new List<NavaidDto>
        {
            new() { NavId = "DFW" },
            new() { NavId = "AUS" }
        };
        _navaidService.GetNavaidsByIdentifiers(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>()).Returns(navaids);

        var result = await _sut.GetBatch("DFW,AUS", CancellationToken.None);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBatch_EmptyIds_ThrowsValidationException()
    {
        var act = () => _sut.GetBatch("", CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*NAVAID identifier is required*");
    }

    [Fact]
    public async Task GetBatch_NullIds_ThrowsValidationException()
    {
        var act = () => _sut.GetBatch(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*NAVAID identifier is required*");
    }

    [Fact]
    public async Task GetBatch_TooManyIds_ThrowsValidationException()
    {
        var ids = string.Join(",", Enumerable.Range(1, 101).Select(i => $"NAV{i}"));

        var act = () => _sut.GetBatch(ids, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Maximum of 100*");
    }

    #endregion

    #region SearchNearby Tests

    [Fact]
    public async Task SearchNearby_HappyPath_ReturnsNavaids()
    {
        var expected = new PaginatedResponse<NavaidDto>
        {
            Data = [new NavaidDto { NavId = "DFW" }],
            Pagination = new PaginationMetadata { Limit = 100, HasMore = false }
        };
        _navaidService.SearchNearby(32.897m, -97.038m, 30, null, null, 100).Returns(expected);

        var result = await _sut.SearchNearby(32.897m, -97.038m);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expected);
    }

    [Fact]
    public async Task SearchNearby_WithTypeFilter_PassesToService()
    {
        var expected = new PaginatedResponse<NavaidDto>
        {
            Data = [],
            Pagination = new PaginationMetadata { Limit = 100, HasMore = false }
        };
        _navaidService.SearchNearby(32.897m, -97.038m, 50, "VOR", null, 100).Returns(expected);

        await _sut.SearchNearby(32.897m, -97.038m, 50, "VOR");

        await _navaidService.Received(1).SearchNearby(32.897m, -97.038m, 50, "VOR", null, 100);
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
        var expected = new PaginatedResponse<NavaidDto>
        {
            Data = [],
            Pagination = new PaginationMetadata { Limit = 100, HasMore = false }
        };
        _navaidService.SearchNearby(32.897m, -97.038m, 30, null, null, 100).Returns(expected);

        await _sut.SearchNearby(32.897m, -97.038m);

        await _navaidService.Received(1).SearchNearby(32.897m, -97.038m, 30, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int>());
    }

    #endregion
}

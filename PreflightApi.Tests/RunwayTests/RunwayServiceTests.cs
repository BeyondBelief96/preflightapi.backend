using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Enums;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Services.AirportInformationServices;
using Xunit;

namespace PreflightApi.Tests.RunwayTests;

public class RunwayServiceTests : IDisposable
{
    private readonly PreflightApiDbContext _dbContext;
    private readonly RunwayService _service;

    public RunwayServiceTests()
    {
        var options = new DbContextOptionsBuilder<PreflightApiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PreflightApiDbContext(options);
        var logger = Substitute.For<ILogger<RunwayService>>();
        _service = new RunwayService(_dbContext, logger);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetRunwaysByAirportAsync Tests

    [Fact]
    public async Task GetRunwaysByAirportAsync_ByIcaoCode_ReturnsRunways()
    {
        SeedAirportsAndRunways();

        var result = (await _service.GetRunwaysByAirportAsync("KDFW")).ToList();

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(r =>
        {
            r.AirportIcaoCode.Should().Be("KDFW");
            r.AirportArptId.Should().Be("DFW");
            r.AirportName.Should().Be("DALLAS-FT WORTH INTL");
        });
    }

    [Fact]
    public async Task GetRunwaysByAirportAsync_ByArptId_ReturnsRunways()
    {
        SeedAirportsAndRunways();

        var result = (await _service.GetRunwaysByAirportAsync("DFW")).ToList();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRunwaysByAirportAsync_CaseInsensitive_ReturnsRunways()
    {
        SeedAirportsAndRunways();

        var result = (await _service.GetRunwaysByAirportAsync("kdfw")).ToList();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRunwaysByAirportAsync_NotFound_ThrowsAirportNotFoundException()
    {
        SeedAirportsAndRunways();

        var act = () => _service.GetRunwaysByAirportAsync("ZZZZ");

        await act.Should().ThrowAsync<AirportNotFoundException>();
    }

    [Fact]
    public async Task GetRunwaysByAirportAsync_OrdersByRunwayId()
    {
        SeedAirportsAndRunways();

        var result = (await _service.GetRunwaysByAirportAsync("KDFW")).ToList();

        result[0].RunwayId.Should().Be("13L/31R");
        result[1].RunwayId.Should().Be("17L/35R");
    }

    #endregion

    #region GetRunways Tests

    [Fact]
    public async Task GetRunways_NoFilters_ReturnsAllRunways()
    {
        SeedAirportsAndRunways();

        var result = await _service.GetRunways(null, null, null, null, null, null, 100);

        result.Data.Should().HaveCount(3); // 2 DFW + 1 AUS
    }

    [Fact]
    public async Task GetRunways_FilterByState_FiltersCorrectly()
    {
        SeedAirportsAndRunways();

        var result = await _service.GetRunways(null, null, null, "TX", null, null, 100);

        result.Data.Should().HaveCount(3); // Both DFW (2) and AUS (1) are in TX
    }

    [Fact]
    public async Task GetRunways_FilterBySurfaceType_FiltersCorrectly()
    {
        SeedAirportsAndRunways();

        var result = await _service.GetRunways(null, RunwaySurfaceType.Concrete, null, null, null, null, 100);

        result.Data.Should().AllSatisfy(r => r.SurfaceType.Should().Be(RunwaySurfaceType.Concrete));
    }

    [Fact]
    public async Task GetRunways_FilterByMinLength_FiltersCorrectly()
    {
        SeedAirportsAndRunways();

        var result = await _service.GetRunways(null, null, 10000, null, null, null, 100);

        result.Data.Should().AllSatisfy(r => r.Length.Should().BeGreaterThanOrEqualTo(10000));
    }

    [Fact]
    public async Task GetRunways_FilterByLighted_FiltersCorrectly()
    {
        SeedAirportsAndRunways();

        var result = await _service.GetRunways(null, null, null, null, true, null, 100);

        result.Data.Should().AllSatisfy(r =>
            r.EdgeLightIntensity.Should().NotBe(RunwayEdgeLightIntensity.None));
    }

    [Fact]
    public async Task GetRunways_CombinedFilters_FiltersCorrectly()
    {
        SeedAirportsAndRunways();

        var result = await _service.GetRunways(null, RunwaySurfaceType.Concrete, 10000, "TX", true, null, 100);

        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRunways_NoMatchingState_ReturnsEmpty()
    {
        SeedAirportsAndRunways();

        var result = await _service.GetRunways(null, null, null, "ZZ", null, null, 100);

        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRunways_Paginates()
    {
        SeedAirportsAndRunways();

        var result = await _service.GetRunways(null, null, null, null, null, null, 2);

        result.Data.Count().Should().Be(2);
        result.Pagination.HasMore.Should().BeTrue();
        result.Pagination.NextCursor.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetRunways_IncludesAirportContext()
    {
        SeedAirportsAndRunways();

        var result = await _service.GetRunways(null, null, null, null, null, null, 100);

        result.Data.Should().AllSatisfy(r =>
        {
            r.AirportArptId.Should().NotBeNullOrEmpty();
            r.AirportName.Should().NotBeNullOrEmpty();
        });
    }

    #endregion

    #region Helpers

    private void SeedAirportsAndRunways()
    {
        var dfwAirport = new Airport
        {
            SiteNo = "50078.*A",
            ArptId = "DFW",
            IcaoId = "KDFW",
            ArptName = "DALLAS-FT WORTH INTL",
            StateCode = "TX",
            City = "DALLAS-FT WORTH"
        };

        var ausAirport = new Airport
        {
            SiteNo = "50085.*A",
            ArptId = "AUS",
            IcaoId = "KAUS",
            ArptName = "AUSTIN-BERGSTROM INTL",
            StateCode = "TX",
            City = "AUSTIN"
        };

        _dbContext.Airports.AddRange(dfwAirport, ausAirport);

        _dbContext.Runways.AddRange(
            new Runway
            {
                Id = Guid.NewGuid(),
                SiteNo = "50078.*A",
                RunwayId = "17L/35R",
                Length = 13401,
                Width = 200,
                SurfaceTypeCode = "CONC",
                EdgeLightIntensity = "HIGH"
            },
            new Runway
            {
                Id = Guid.NewGuid(),
                SiteNo = "50078.*A",
                RunwayId = "13L/31R",
                Length = 11388,
                Width = 200,
                SurfaceTypeCode = "CONC",
                EdgeLightIntensity = "HIGH"
            },
            new Runway
            {
                Id = Guid.NewGuid(),
                SiteNo = "50085.*A",
                RunwayId = "17L/35R",
                Length = 12248,
                Width = 150,
                SurfaceTypeCode = "ASPH",
                EdgeLightIntensity = "HIGH"
            });

        _dbContext.SaveChanges();
    }

    #endregion
}

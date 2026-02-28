using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Services.AirportInformationServices;
using Xunit;

namespace PreflightApi.Tests.NavaidTests;

public class NavaidServiceTests : IDisposable
{
    private readonly PreflightApiDbContext _dbContext;
    private readonly NavaidService _service;

    public NavaidServiceTests()
    {
        var options = new DbContextOptionsBuilder<PreflightApiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PreflightApiDbContext(options);
        var logger = Substitute.For<ILogger<NavaidService>>();
        _service = new NavaidService(_dbContext, logger);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetNavaids Tests

    [Fact]
    public async Task GetNavaids_ReturnsAll_WhenNoFilters()
    {
        SeedNavaids();

        var result = await _service.GetNavaids(null, null, null, null, 100);

        result.Data.Should().HaveCount(3);
        result.Pagination.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task GetNavaids_FiltersByNavType()
    {
        SeedNavaids();

        var result = await _service.GetNavaids(null, "VOR", null, null, 100);

        result.Data.Should().HaveCount(1);
        result.Data.First().NavType.Should().Be(NavaidType.Vor);
    }

    [Fact]
    public async Task GetNavaids_FiltersByNavType_CaseInsensitive()
    {
        SeedNavaids();

        var result = await _service.GetNavaids(null, "vor", null, null, 100);

        result.Data.Should().HaveCount(1);
        result.Data.First().NavType.Should().Be(NavaidType.Vor);
    }

    [Fact]
    public async Task GetNavaids_FiltersByState()
    {
        SeedNavaids();

        var result = await _service.GetNavaids(null, null, "TX", null, 100);

        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetNavaids_FiltersByState_CaseInsensitive()
    {
        SeedNavaids();

        var result = await _service.GetNavaids(null, null, "tx", null, 100);

        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetNavaids_CombinesFilters()
    {
        SeedNavaids();

        var result = await _service.GetNavaids(null, "VORTAC", "TX", null, 100);

        result.Data.Should().HaveCount(1);
        result.Data.First().NavId.Should().Be("DFW");
    }

    [Fact]
    public async Task GetNavaids_PaginatesResults()
    {
        SeedNavaids();

        var result = await _service.GetNavaids(null, null, null, null, 2);

        result.Data.Should().HaveCount(2);
        result.Pagination.HasMore.Should().BeTrue();
        result.Pagination.NextCursor.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetNavaids_ReturnsEmpty_WhenNoMatches()
    {
        SeedNavaids();

        var result = await _service.GetNavaids(null, "TACAN", null, null, 100);

        result.Data.Should().BeEmpty();
        result.Pagination.HasMore.Should().BeFalse();
    }

    #endregion

    #region GetNavaidsByIdentifier Tests

    [Fact]
    public async Task GetNavaidsByIdentifier_ReturnsMatches()
    {
        SeedNavaids();

        var result = (await _service.GetNavaidsByIdentifier("DFW")).ToList();

        result.Should().HaveCount(1);
        result[0].NavId.Should().Be("DFW");
    }

    [Fact]
    public async Task GetNavaidsByIdentifier_IsCaseInsensitive()
    {
        SeedNavaids();

        var result = (await _service.GetNavaidsByIdentifier("dfw")).ToList();

        result.Should().HaveCount(1);
        result[0].NavId.Should().Be("DFW");
    }

    [Fact]
    public async Task GetNavaidsByIdentifier_ReturnsEmpty_WhenNotFound()
    {
        SeedNavaids();

        var result = await _service.GetNavaidsByIdentifier("ZZZ");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNavaidsByIdentifier_ReturnsMultiple_WhenSameNavId()
    {
        // Seed two navaids with same NavId but different types
        _dbContext.Navaids.AddRange(
            CreateNavaid("TST", "VOR", "TX", "TEST CITY"),
            CreateNavaid("TST", "NDB", "TX", "TEST CITY")
        );
        await _dbContext.SaveChangesAsync();

        var result = (await _service.GetNavaidsByIdentifier("TST")).ToList();

        result.Should().HaveCount(2);
        result.Select(n => n.NavType).Should().Contain(NavaidType.Vor).And.Contain(NavaidType.Ndb);
    }

    #endregion

    #region GetNavaidsByIdentifiers (Batch) Tests

    [Fact]
    public async Task GetNavaidsByIdentifiers_ReturnsMatches()
    {
        SeedNavaids();

        var result = (await _service.GetNavaidsByIdentifiers(["DFW", "AUS"])).ToList();

        result.Should().HaveCount(2);
        result.Select(n => n.NavId).Should().Contain("DFW").And.Contain("AUS");
    }

    [Fact]
    public async Task GetNavaidsByIdentifiers_ReturnsEmpty_ForEmptyInput()
    {
        SeedNavaids();

        var result = await _service.GetNavaidsByIdentifiers([]);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNavaidsByIdentifiers_IsCaseInsensitive()
    {
        SeedNavaids();

        var result = (await _service.GetNavaidsByIdentifiers(["dfw", "aus"])).ToList();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetNavaidsByIdentifiers_IgnoresDuplicateInputIds()
    {
        SeedNavaids();

        var result = (await _service.GetNavaidsByIdentifiers(["DFW", "DFW", "dfw"])).ToList();

        result.Should().HaveCount(1);
    }

    #endregion

    private void SeedNavaids()
    {
        _dbContext.Navaids.AddRange(
            CreateNavaid("DFW", "VORTAC", "TX", "DALLAS"),
            CreateNavaid("AUS", "VOR", "TX", "AUSTIN"),
            CreateNavaid("BIE", "NDB", "NE", "BEATRICE")
        );
        _dbContext.SaveChanges();
    }

    private static Navaid CreateNavaid(string navId, string navType, string stateCode, string city)
    {
        return new Navaid
        {
            Id = Guid.NewGuid(),
            NavId = navId,
            NavType = navType,
            NavStatus = "OPERATIONAL",
            Name = $"{city} {navType}",
            City = city,
            StateCode = stateCode,
            StateName = stateCode == "TX" ? "TEXAS" : "NEBRASKA",
            CountryCode = "US",
            CountryName = "UNITED STATES",
            NasUseFlag = "Y",
            PublicUseFlag = "Y",
            LatDecimal = 32.89680000m,
            LongDecimal = -97.03800000m,
            EffectiveDate = new DateTime(2024, 1, 25)
        };
    }
}

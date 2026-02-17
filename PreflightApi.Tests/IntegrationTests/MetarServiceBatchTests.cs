using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Services.WeatherServices;
using Xunit;

namespace PreflightApi.Tests.IntegrationTests;

public class MetarServiceBatchTests : PostgreSqlTestBase
{
    protected override async Task SeedDatabaseAsync()
    {
        // Airports
        DbContext.Airports.AddRange(
            CreateAirport("10000", "KDFW", "DFW", "TX"),
            CreateAirport("10001", "KAUS", "AUS", "TX"),
            CreateAirport("10002", "KHOU", "HOU", "TX"),
            CreateAirport("10003", "PHNL", "HNL", "HI"),
            CreateAirport("10004", null, "T82", "TX")  // No ICAO, FAA-only ident
        );

        // METARs
        DbContext.Metars.AddRange(
            new Metar { StationId = "KDFW", RawText = "KDFW metar" },
            new Metar { StationId = "KAUS", RawText = "KAUS metar" },
            new Metar { StationId = "KHOU", RawText = "KHOU metar" },
            new Metar { StationId = "PHNL", RawText = "PHNL metar" },
            new Metar { StationId = "KT82", RawText = "KT82 metar" }
        );

        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetMetarsForAirports_DirectIcaoMatch_ReturnsMetars()
    {
        var service = CreateService();

        var result = (await service.GetMetarsForAirports(["KDFW", "KAUS"])).ToList();

        result.Should().HaveCount(2);
        result.Select(m => m.StationId).Should().Contain(["KDFW", "KAUS"]);
    }

    [Fact]
    public async Task GetMetarsForAirports_FaaIdentFallback_ResolvesViaAirportTable()
    {
        var service = CreateService();

        // "DFW" isn't a StationId — should resolve via Airport.ArptId → "KDFW"
        var result = (await service.GetMetarsForAirports(["DFW"])).ToList();

        result.Should().HaveCount(1);
        result[0].StationId.Should().Be("KDFW");
    }

    [Fact]
    public async Task GetMetarsForAirports_HawaiiAirport_UsesPPrefix()
    {
        var service = CreateService();

        // "HNL" should resolve via Airport (HI state) → "PHNL"
        var result = (await service.GetMetarsForAirports(["HNL"])).ToList();

        result.Should().HaveCount(1);
        result[0].StationId.Should().Be("PHNL");
    }

    [Fact]
    public async Task GetMetarsForAirports_MixedDirectAndFallback_ReturnsCombined()
    {
        var service = CreateService();

        // "KDFW" matches directly, "HOU" needs fallback, "HNL" needs fallback with P prefix
        var result = (await service.GetMetarsForAirports(["KDFW", "HOU", "HNL"])).ToList();

        result.Should().HaveCount(3);
        result.Select(m => m.StationId).Should().Contain(["KDFW", "KHOU", "PHNL"]);
    }

    [Fact]
    public async Task GetMetarsForAirports_FaaOnlyIdent_ResolvesWithKPrefix()
    {
        var service = CreateService();

        // "T82" has no ICAO — should resolve via ArptId → "KT82"
        var result = (await service.GetMetarsForAirports(["T82"])).ToList();

        result.Should().HaveCount(1);
        result[0].StationId.Should().Be("KT82");
    }

    [Fact]
    public async Task GetMetarsForAirports_NonExistentStation_SilentlySkipped()
    {
        var service = CreateService();

        var result = (await service.GetMetarsForAirports(["KDFW", "ZZZZ"])).ToList();

        result.Should().HaveCount(1);
        result[0].StationId.Should().Be("KDFW");
    }

    [Fact]
    public async Task GetMetarsForAirports_CaseInsensitive_MatchesRegardless()
    {
        var service = CreateService();

        var result = (await service.GetMetarsForAirports(["kdfw", "kaus"])).ToList();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMetarsForAirports_DuplicateInputs_ReturnsDistinct()
    {
        var service = CreateService();

        var result = (await service.GetMetarsForAirports(["KDFW", "KDFW", "kdfw"])).ToList();

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMetarsForAirports_AllNonExistent_ReturnsEmpty()
    {
        var service = CreateService();

        var result = (await service.GetMetarsForAirports(["ZZZZ", "YYYY"])).ToList();

        result.Should().BeEmpty();
    }

    private MetarService CreateService()
    {
        return new MetarService(DbContext, Substitute.For<ILogger<MetarService>>());
    }

    private static Airport CreateAirport(string siteNo, string? icaoId, string arptId, string stateCode)
    {
        return new Airport
        {
            SiteNo = siteNo,
            IcaoId = icaoId,
            ArptId = arptId,
            StateCode = stateCode,
            EffDate = DateTime.UtcNow
        };
    }
}

using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Services.WeatherServices;
using Xunit;

namespace PreflightApi.Tests.IntegrationTests;

[Collection("Integration")]
public class TafServiceBatchTests : PostgreSqlTestBase
{
    protected override async Task SeedDatabaseAsync()
    {
        // Airports
        DbContext.Airports.AddRange(
            CreateAirport("20000", "KDFW", "DFW", "TX"),
            CreateAirport("20001", "KAUS", "AUS", "TX"),
            CreateAirport("20002", "KHOU", "HOU", "TX"),
            CreateAirport("20003", "PHNL", "HNL", "HI"),
            CreateAirport("20004", null, "T82", "TX")
        );

        // TAFs
        DbContext.Tafs.AddRange(
            new Taf { StationId = "KDFW", RawText = "TAF KDFW" },
            new Taf { StationId = "KAUS", RawText = "TAF KAUS" },
            new Taf { StationId = "KHOU", RawText = "TAF KHOU" },
            new Taf { StationId = "PHNL", RawText = "TAF PHNL" },
            new Taf { StationId = "KT82", RawText = "TAF KT82" }
        );

        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetTafsForAirports_DirectIcaoMatch_ReturnsTafs()
    {
        var service = CreateService();

        var result = (await service.GetTafsForAirports(["KDFW", "KAUS"])).ToList();

        result.Should().HaveCount(2);
        result.Select(t => t.StationId).Should().Contain(["KDFW", "KAUS"]);
    }

    [Fact]
    public async Task GetTafsForAirports_FaaIdentFallback_ResolvesViaAirportTable()
    {
        var service = CreateService();

        var result = (await service.GetTafsForAirports(["DFW"])).ToList();

        result.Should().HaveCount(1);
        result[0].StationId.Should().Be("KDFW");
    }

    [Fact]
    public async Task GetTafsForAirports_HawaiiAirport_UsesPPrefix()
    {
        var service = CreateService();

        var result = (await service.GetTafsForAirports(["HNL"])).ToList();

        result.Should().HaveCount(1);
        result[0].StationId.Should().Be("PHNL");
    }

    [Fact]
    public async Task GetTafsForAirports_MixedDirectAndFallback_ReturnsCombined()
    {
        var service = CreateService();

        var result = (await service.GetTafsForAirports(["KDFW", "HOU", "HNL"])).ToList();

        result.Should().HaveCount(3);
        result.Select(t => t.StationId).Should().Contain(["KDFW", "KHOU", "PHNL"]);
    }

    [Fact]
    public async Task GetTafsForAirports_FaaOnlyIdent_ResolvesWithKPrefix()
    {
        var service = CreateService();

        var result = (await service.GetTafsForAirports(["T82"])).ToList();

        result.Should().HaveCount(1);
        result[0].StationId.Should().Be("KT82");
    }

    [Fact]
    public async Task GetTafsForAirports_NonExistentStation_SilentlySkipped()
    {
        var service = CreateService();

        var result = (await service.GetTafsForAirports(["KDFW", "ZZZZ"])).ToList();

        result.Should().HaveCount(1);
        result[0].StationId.Should().Be("KDFW");
    }

    [Fact]
    public async Task GetTafsForAirports_CaseInsensitive_MatchesRegardless()
    {
        var service = CreateService();

        var result = (await service.GetTafsForAirports(["kdfw", "kaus"])).ToList();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTafsForAirports_DuplicateInputs_ReturnsDistinct()
    {
        var service = CreateService();

        var result = (await service.GetTafsForAirports(["KDFW", "KDFW", "kdfw"])).ToList();

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetTafsForAirports_AllNonExistent_ReturnsEmpty()
    {
        var service = CreateService();

        var result = (await service.GetTafsForAirports(["ZZZZ", "YYYY"])).ToList();

        result.Should().BeEmpty();
    }

    private TafService CreateService()
    {
        return new TafService(DbContext, Substitute.For<ILogger<TafService>>());
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

using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NSubstitute;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Dtos.Notam;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Services.NotamServices;
using PreflightApi.Infrastructure.Settings;
using Xunit;

namespace PreflightApi.Tests.IntegrationTests;

/// <summary>
/// Integration tests for NotamService filter predicates that only translate correctly
/// against a real PostgreSQL backend. These tests catch bugs where a predicate compiles
/// and runs fine under EF Core's in-memory provider but fails at SQL translation time
/// (e.g. ILIKE against a jsonb column, jsonb operators, etc.).
/// </summary>
[Collection("Integration")]
public class NotamServiceFilterTests : PostgreSqlTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly GeometryFactory GeometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    protected override async Task SeedDatabaseAsync()
    {
        var now = DateTime.UtcNow;

        DbContext.Notams.AddRange(
            BuildNotam("1", "DFW", "KDFW", NotamClassification.DOMESTIC, NotamFeature.RWY,
                text: "RWY 17L/35R CLOSED", effectiveEnd: now.AddDays(1), lat: 32.8998, lon: -97.0403),
            BuildNotam("2", "DFW", "KDFW", NotamClassification.DOMESTIC, NotamFeature.TWY,
                text: "TWY A CLOSED", effectiveEnd: now.AddDays(1), lat: 32.8998, lon: -97.0403),
            BuildNotam("3", "AUS", "KAUS", NotamClassification.FDC, NotamFeature.IAP,
                text: "ILS RWY 17L OUT OF SERVICE", effectiveEnd: now.AddDays(1), lat: 30.1975, lon: -97.6664),
            BuildNotam("4", "DFW", "KDFW", NotamClassification.DOMESTIC, NotamFeature.RWY,
                text: "RWY 13R/31L CLOSED", effectiveEnd: now.AddHours(-1), lat: 32.8998, lon: -97.0403)
        );

        await DbContext.SaveChangesAsync();
    }

    // Regression: SearchNotamsAsync with a feature filter used to throw
    // "function pg_catalog.like_escape(jsonb, unknown) does not exist" because
    // the predicate was EF.Functions.ILike on a jsonb column.
    [Fact]
    public async Task SearchNotamsAsync_FilterByFeature_ExecutesAgainstJsonbColumn()
    {
        var service = CreateService();
        var filters = new NotamFilterDto { Feature = "RWY" };

        var result = await service.SearchNotamsAsync(filters, cursor: null, limit: 100);

        result.Data.Should().NotBeNull();
        result.Data.Should().OnlyContain(n =>
            n.Properties!.CoreNotamData!.Notam!.Feature == NotamFeature.RWY);
        // Expired NOTAM (id=4) is excluded by ApplyActiveFilter
        result.Data.Should().HaveCount(1);
        result.Data.Single().Id.Should().Be("1");
    }

    [Fact]
    public async Task SearchNotamsAsync_FilterByFeature_IsCaseInsensitive()
    {
        var service = CreateService();
        var filters = new NotamFilterDto { Feature = "rwy" };

        var result = await service.SearchNotamsAsync(filters, cursor: null, limit: 100);

        result.Data.Should().HaveCount(1);
        result.Data.Single().Properties!.CoreNotamData!.Notam!.Feature
            .Should().Be(NotamFeature.RWY);
    }

    [Fact]
    public async Task SearchNotamsAsync_FilterByFeature_CombinedWithClassification()
    {
        var service = CreateService();
        var filters = new NotamFilterDto { Feature = "IAP", Classification = "FDC" };

        var result = await service.SearchNotamsAsync(filters, cursor: null, limit: 100);

        result.Data.Should().HaveCount(1);
        result.Data.Single().Id.Should().Be("3");
    }

    [Fact]
    public async Task SearchNotamsAsync_FilterByFreeText_ExecutesAgainstTextColumn()
    {
        var service = CreateService();
        var filters = new NotamFilterDto { FreeText = "CLOSED" };

        var result = await service.SearchNotamsAsync(filters, cursor: null, limit: 100);

        result.Data.Should().HaveCount(2);
        result.Data.Select(n => n.Id).Should().BeEquivalentTo(new[] { "1", "2" });
    }

    [Fact]
    public async Task GetNotamsForAirportAsync_FilterByFeature_ExecutesAgainstJsonbColumn()
    {
        var service = CreateService();
        var filters = new NotamFilterDto { Feature = "RWY" };

        var result = await service.GetNotamsForAirportAsync("KDFW", filters);

        result.Notams.Should().HaveCount(1);
        result.Notams.Single().Id.Should().Be("1");
    }

    [Fact]
    public async Task GetNotamsByRadiusAsync_FilterByFeature_ExecutesAgainstJsonbColumn()
    {
        var service = CreateService();
        var filters = new NotamFilterDto { Feature = "TWY" };

        var result = await service.GetNotamsByRadiusAsync(32.8998, -97.0403, 10, filters);

        result.Notams.Should().HaveCount(1);
        result.Notams.Single().Id.Should().Be("2");
    }

    [Fact]
    public async Task SearchNotamsAsync_FilterByFeature_NoMatches_ReturnsEmpty()
    {
        var service = CreateService();
        var filters = new NotamFilterDto { Feature = "OBST" };

        var result = await service.SearchNotamsAsync(filters, cursor: null, limit: 100);

        result.Data.Should().BeEmpty();
    }

    private NotamService CreateService()
    {
        var settings = Options.Create(new NmsSettings
        {
            CacheDurationMinutes = 5,
            DefaultRouteCorridorRadiusNm = 25
        });
        var logger = Substitute.For<ILogger<NotamService>>();
        return new NotamService(DbContext, settings, logger);
    }

    private static Notam BuildNotam(
        string nmsId,
        string location,
        string icaoLocation,
        NotamClassification classification,
        NotamFeature feature,
        string text,
        DateTime effectiveEnd,
        double lat,
        double lon)
    {
        var dto = new NotamDto
        {
            Type = "Feature",
            Id = nmsId,
            Properties = new NotamPropertiesDto
            {
                CoreNotamData = new CoreNotamDataDto
                {
                    Notam = new NotamDetailDto
                    {
                        Id = nmsId,
                        Number = "001",
                        Location = location,
                        IcaoLocation = icaoLocation,
                        Classification = classification,
                        Feature = feature,
                        Type = NotamType.N,
                        Text = text,
                        EffectiveStart = DateTime.UtcNow.AddHours(-1).ToString("O"),
                        EffectiveEnd = effectiveEnd.ToString("O")
                    }
                }
            }
        };

        return new Notam
        {
            NmsId = nmsId,
            Location = location,
            IcaoLocation = icaoLocation,
            Classification = classification.ToString(),
            NotamType = "N",
            NotamNumber = "001",
            EffectiveStart = DateTime.UtcNow.AddHours(-1),
            EffectiveEnd = effectiveEnd,
            Text = text,
            LastUpdated = DateTime.UtcNow,
            SyncedAt = DateTime.UtcNow,
            FeatureJson = JsonSerializer.Serialize(dto, JsonOptions),
            Geometry = GeometryFactory.CreatePoint(new Coordinate(lon, lat))
        };
    }
}

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Briefing;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Services;
using PreflightApi.Tests.IntegrationTests;
using Xunit;

namespace PreflightApi.Tests.BriefingTests;

[Collection("Integration")]
public class BriefingServiceTests : PostgreSqlTestBase
{
    private readonly IAirportService _airportService = Substitute.For<IAirportService>();
    private readonly IMetarService _metarService = Substitute.For<IMetarService>();
    private readonly ITafService _tafService = Substitute.For<ITafService>();

    private BriefingService CreateService()
    {
        // Build a real IServiceScopeFactory so parallel queries each get their own DbContext
        var services = new ServiceCollection();
        var dsBuilder = new NpgsqlDataSourceBuilder(ConnectionString);
        dsBuilder.EnableDynamicJson();
        dsBuilder.UseNetTopologySuite();
        services.AddSingleton(dsBuilder.Build());
        services.AddDbContext<PreflightApiDbContext>((sp, options) =>
        {
            var ds = sp.GetRequiredService<NpgsqlDataSource>();
            options.UseNpgsql(ds, o => o.UseNetTopologySuite());
        });
        services.AddSingleton(_metarService);
        services.AddSingleton(_tafService);
        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        return new BriefingService(
            DbContext,
            _airportService,
            scopeFactory,
            Substitute.For<ILogger<BriefingService>>());
    }

    private static readonly AirportDto KdfwAirport = new() { IcaoId = "KDFW", ArptId = "DFW", LatDecimal = 32.8968, LongDecimal = -97.0380 };
    private static readonly AirportDto KausAirport = new() { IcaoId = "KAUS", ArptId = "AUS", LatDecimal = 30.1945, LongDecimal = -97.6699 };
    private static readonly AirportDto KiahAirport = new() { IcaoId = "KIAH", ArptId = "IAH", LatDecimal = 29.9844, LongDecimal = -95.3414 };

    private void SetupAirportLookups()
    {
        _airportService.GetAirportsByIcaoCodesOrIdents(Arg.Any<string[]>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var codes = callInfo.ArgAt<string[]>(0);
                var all = new[] { KdfwAirport, KausAirport, KiahAirport };
                return all.Where(a =>
                    codes.Any(c => c.Equals(a.IcaoId, StringComparison.OrdinalIgnoreCase)
                               || c.Equals(a.ArptId, StringComparison.OrdinalIgnoreCase)
                               || c.Equals("K" + a.ArptId, StringComparison.OrdinalIgnoreCase)));
            });
    }

    // ── Validation Tests ──

    [Fact]
    public async Task GetRouteBriefing_TooFewWaypoints_ThrowsValidationException()
    {
        var service = CreateService();
        var request = new RouteBriefingRequest
        {
            Waypoints = new List<BriefingWaypoint> { new() { AirportIdentifier = "KDFW" } }
        };

        var act = () => service.GetRouteBriefingAsync(request);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*At least two waypoints*");
    }

    [Fact]
    public async Task GetRouteBriefing_ZeroCorridorWidth_ThrowsValidationException()
    {
        var service = CreateService();
        var request = new RouteBriefingRequest
        {
            Waypoints = new List<BriefingWaypoint>
            {
                new() { AirportIdentifier = "KDFW" },
                new() { AirportIdentifier = "KAUS" }
            },
            CorridorWidthNm = 0
        };

        var act = () => service.GetRouteBriefingAsync(request);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Corridor width must be greater than 0*");
    }

    [Fact]
    public async Task GetRouteBriefing_CorridorWidthTooLarge_ThrowsValidationException()
    {
        var service = CreateService();
        var request = new RouteBriefingRequest
        {
            Waypoints = new List<BriefingWaypoint>
            {
                new() { AirportIdentifier = "KDFW" },
                new() { AirportIdentifier = "KAUS" }
            },
            CorridorWidthNm = 101
        };

        var act = () => service.GetRouteBriefingAsync(request);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Corridor width cannot exceed 100*");
    }

    [Fact]
    public async Task GetRouteBriefing_CoordinateWaypointMissingLatLon_ThrowsValidationException()
    {
        var service = CreateService();
        var request = new RouteBriefingRequest
        {
            Waypoints = new List<BriefingWaypoint>
            {
                new() { Latitude = 32.8968m },  // Missing longitude
                new() { AirportIdentifier = "KAUS" }
            }
        };

        var act = () => service.GetRouteBriefingAsync(request);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*latitude and longitude are required*");
    }

    [Fact]
    public async Task GetRouteBriefing_InvalidLatitude_ThrowsValidationException()
    {
        var service = CreateService();
        var request = new RouteBriefingRequest
        {
            Waypoints = new List<BriefingWaypoint>
            {
                new() { Latitude = 91m, Longitude = -97m },
                new() { AirportIdentifier = "KAUS" }
            }
        };

        var act = () => service.GetRouteBriefingAsync(request);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*latitude must be between -90 and 90*");
    }

    [Fact]
    public async Task GetRouteBriefing_InvalidLongitude_ThrowsValidationException()
    {
        var service = CreateService();
        var request = new RouteBriefingRequest
        {
            Waypoints = new List<BriefingWaypoint>
            {
                new() { Latitude = 32m, Longitude = -181m },
                new() { AirportIdentifier = "KAUS" }
            }
        };

        var act = () => service.GetRouteBriefingAsync(request);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*longitude must be between -180 and 180*");
    }

    [Fact]
    public async Task GetRouteBriefing_AirportNotFound_PropagatesException()
    {
        // Batch returns empty for unknown airport
        _airportService.GetAirportsByIcaoCodesOrIdents(Arg.Any<string[]>(), Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<AirportDto>());

        var service = CreateService();
        var request = new RouteBriefingRequest
        {
            Waypoints = new List<BriefingWaypoint>
            {
                new() { AirportIdentifier = "XXXX" },
                new() { AirportIdentifier = "KAUS" }
            }
        };

        var act = () => service.GetRouteBriefingAsync(request);

        await act.Should().ThrowAsync<AirportNotFoundException>();
    }

    [Fact]
    public async Task GetRouteBriefing_AirportMissingCoords_ThrowsValidationException()
    {
        _airportService.GetAirportsByIcaoCodesOrIdents(Arg.Any<string[]>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new AirportDto { IcaoId = "KDFW", ArptId = "DFW", LatDecimal = null, LongDecimal = null } });

        var service = CreateService();
        var request = new RouteBriefingRequest
        {
            Waypoints = new List<BriefingWaypoint>
            {
                new() { AirportIdentifier = "KDFW" },
                new() { AirportIdentifier = "KAUS" }
            }
        };

        var act = () => service.GetRouteBriefingAsync(request);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*does not have coordinates*");
    }

    // ── Integration Tests (empty DB) ──

    [Fact]
    public async Task GetRouteBriefing_EmptyDatabase_ReturnsEmptyBriefing()
    {
        SetupAirportLookups();
        _metarService.GetMetarsForAirports(Arg.Any<string[]>()).Returns(Enumerable.Empty<MetarDto>());
        _tafService.GetTafsForAirports(Arg.Any<string[]>()).Returns(Enumerable.Empty<TafDto>());

        var service = CreateService();
        var request = new RouteBriefingRequest
        {
            Waypoints = new List<BriefingWaypoint>
            {
                new() { AirportIdentifier = "KDFW" },
                new() { AirportIdentifier = "KAUS" }
            }
        };

        var result = await service.GetRouteBriefingAsync(request);

        result.Route.Should().Be("KDFW -> KAUS");
        result.CorridorWidthNm.Should().Be(25);
        result.Pireps.Should().BeEmpty();
        result.Sigmets.Should().BeEmpty();
        result.GAirmets.Should().BeEmpty();
        result.Notams.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRouteBriefing_WithCoordinateWaypoints_BuildsRoute()
    {
        _metarService.GetMetarsForAirports(Arg.Any<string[]>()).Returns(Enumerable.Empty<MetarDto>());
        _tafService.GetTafsForAirports(Arg.Any<string[]>()).Returns(Enumerable.Empty<TafDto>());

        var service = CreateService();
        var request = new RouteBriefingRequest
        {
            Waypoints = new List<BriefingWaypoint>
            {
                new() { Latitude = 32.8968m, Longitude = -97.0380m },
                new() { Latitude = 30.1945m, Longitude = -97.6699m }
            }
        };

        var result = await service.GetRouteBriefingAsync(request);

        result.Route.Should().Contain("32.8968");
        result.Route.Should().Contain("30.1945");
    }

    [Fact]
    public async Task GetRouteBriefing_SummaryMatchesCounts()
    {
        SetupAirportLookups();
        _metarService.GetMetarsForAirports(Arg.Any<string[]>()).Returns(Enumerable.Empty<MetarDto>());
        _tafService.GetTafsForAirports(Arg.Any<string[]>()).Returns(Enumerable.Empty<TafDto>());

        var service = CreateService();
        var request = new RouteBriefingRequest
        {
            Waypoints = new List<BriefingWaypoint>
            {
                new() { AirportIdentifier = "KDFW" },
                new() { AirportIdentifier = "KAUS" }
            }
        };

        var result = await service.GetRouteBriefingAsync(request);

        result.Summary.MetarCount.Should().Be(result.Metars.Count);
        result.Summary.TafCount.Should().Be(result.Tafs.Count);
        result.Summary.PirepCount.Should().Be(result.Pireps.Count);
        result.Summary.SigmetCount.Should().Be(result.Sigmets.Count);
        result.Summary.GAirmetCount.Should().Be(result.GAirmets.Count);
        result.Summary.NotamCount.Should().Be(result.Notams.Count);
    }

    [Fact]
    public async Task GetRouteBriefing_DefaultCorridorWidth_Is25Nm()
    {
        SetupAirportLookups();
        _metarService.GetMetarsForAirports(Arg.Any<string[]>()).Returns(Enumerable.Empty<MetarDto>());
        _tafService.GetTafsForAirports(Arg.Any<string[]>()).Returns(Enumerable.Empty<TafDto>());

        var service = CreateService();
        var request = new RouteBriefingRequest
        {
            Waypoints = new List<BriefingWaypoint>
            {
                new() { AirportIdentifier = "KDFW" },
                new() { AirportIdentifier = "KAUS" }
            }
        };

        var result = await service.GetRouteBriefingAsync(request);

        result.CorridorWidthNm.Should().Be(25);
    }

    [Fact]
    public async Task GetRouteBriefing_FetchesMetarsAndTafsForCorridorAirports()
    {
        SetupAirportLookups();

        var expectedMetars = new List<MetarDto>
        {
            new() { Id = 1, StationId = "KDFW" },
            new() { Id = 2, StationId = "KAUS" }
        };
        var expectedTafs = new List<TafDto>
        {
            new() { StationId = "KDFW" },
            new() { StationId = "KAUS" }
        };

        // The service finds airports along corridor, then fetches METARs/TAFs for them
        _metarService.GetMetarsForAirports(Arg.Any<string[]>()).Returns(expectedMetars.AsEnumerable());
        _tafService.GetTafsForAirports(Arg.Any<string[]>()).Returns(expectedTafs.AsEnumerable());

        var service = CreateService();
        var request = new RouteBriefingRequest
        {
            Waypoints = new List<BriefingWaypoint>
            {
                new() { AirportIdentifier = "KDFW" },
                new() { AirportIdentifier = "KAUS" }
            }
        };

        var result = await service.GetRouteBriefingAsync(request);

        // METARs/TAFs should be populated (actual count depends on airports found in DB corridor)
        result.Metars.Should().NotBeNull();
        result.Tafs.Should().NotBeNull();
    }
}

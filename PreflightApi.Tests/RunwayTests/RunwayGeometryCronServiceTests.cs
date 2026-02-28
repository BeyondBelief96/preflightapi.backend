using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Services.CronJobServices.ArcGisServices;
using RichardSzalay.MockHttp;
using Xunit;

namespace PreflightApi.Tests.RunwayTests;

public class RunwayGeometryCronServiceTests : IDisposable
{
    private readonly PreflightApiDbContext _dbContext;
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly RunwayGeometryCronService _service;

    public RunwayGeometryCronServiceTests()
    {
        var options = new DbContextOptionsBuilder<PreflightApiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PreflightApiDbContext(options);
        _mockHttp = new MockHttpMessageHandler();

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient("ArcGis")
            .Returns(_ => _mockHttp.ToHttpClient());

        var logger = Substitute.For<ILogger<RunwayGeometryCronService>>();
        _service = new RunwayGeometryCronService(logger, httpClientFactory, _dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _mockHttp.Dispose();
    }

    [Fact]
    public async Task UpdateRunwayGeometriesAsync_MatchesRunwaysAndUpdatesGeometry()
    {
        // Arrange
        SeedLocalData();
        SetupAirportLookupResponse(new[]
        {
            new { GLOBAL_ID = "guid-1", IDENT = "DFW" }
        });
        SetupRunwayResponse(new[]
        {
            new
            {
                OBJECTID = 1,
                AIRPORT_ID = "guid-1",
                DESIGNATOR = "17L/35R"
            }
        }, withGeometry: true);

        // Act
        await _service.UpdateRunwayGeometriesAsync();

        // Assert
        var runway = await _dbContext.Runways.FirstAsync(r => r.RunwayId == "17L/35R");
        runway.Geometry.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateRunwayGeometriesAsync_SkipsWhenAirportGuidNotFound()
    {
        // Arrange
        SeedLocalData();
        SetupAirportLookupResponse(Array.Empty<object>()); // No airport lookup results
        SetupRunwayResponse(new[]
        {
            new
            {
                OBJECTID = 1,
                AIRPORT_ID = "unknown-guid",
                DESIGNATOR = "17L/35R"
            }
        }, withGeometry: true);

        // Act
        await _service.UpdateRunwayGeometriesAsync();

        // Assert
        var runway = await _dbContext.Runways.FirstAsync(r => r.RunwayId == "17L/35R");
        runway.Geometry.Should().BeNull();
    }

    [Fact]
    public async Task UpdateRunwayGeometriesAsync_SkipsWhenArptIdNotFoundLocally()
    {
        // Arrange
        SeedLocalData();
        SetupAirportLookupResponse(new[]
        {
            new { GLOBAL_ID = "guid-1", IDENT = "NONEXIST" } // No local airport with this ArptId
        });
        SetupRunwayResponse(new[]
        {
            new
            {
                OBJECTID = 1,
                AIRPORT_ID = "guid-1",
                DESIGNATOR = "17L/35R"
            }
        }, withGeometry: true);

        // Act
        await _service.UpdateRunwayGeometriesAsync();

        // Assert
        var runway = await _dbContext.Runways.FirstAsync(r => r.RunwayId == "17L/35R");
        runway.Geometry.Should().BeNull();
    }

    [Fact]
    public async Task UpdateRunwayGeometriesAsync_SkipsWhenRunwayDesignatorNotFound()
    {
        // Arrange
        SeedLocalData();
        SetupAirportLookupResponse(new[]
        {
            new { GLOBAL_ID = "guid-1", IDENT = "DFW" }
        });
        SetupRunwayResponse(new[]
        {
            new
            {
                OBJECTID = 1,
                AIRPORT_ID = "guid-1",
                DESIGNATOR = "09/27" // Doesn't exist locally
            }
        }, withGeometry: true);

        // Act
        await _service.UpdateRunwayGeometriesAsync();

        // Assert
        var runway = await _dbContext.Runways.FirstAsync(r => r.RunwayId == "17L/35R");
        runway.Geometry.Should().BeNull();
    }

    [Fact]
    public async Task UpdateRunwayGeometriesAsync_SkipsNullAirportIdOrDesignator()
    {
        // Arrange
        SeedLocalData();
        SetupAirportLookupResponse(new[]
        {
            new { GLOBAL_ID = "guid-1", IDENT = "DFW" }
        });
        // Feature with null fields
        SetupRunwayResponseRaw(@"{
            ""features"": [
                {
                    ""attributes"": { ""OBJECTID"": 1, ""AIRPORT_ID"": null, ""DESIGNATOR"": ""17L/35R"" },
                    ""geometry"": { ""rings"": [[[-97.04,32.89],[-97.03,32.89],[-97.03,32.88],[-97.04,32.88],[-97.04,32.89]]] }
                }
            ],
            ""exceededTransferLimit"": false
        }");

        // Act
        await _service.UpdateRunwayGeometriesAsync();

        // Assert
        var runway = await _dbContext.Runways.FirstAsync(r => r.RunwayId == "17L/35R");
        runway.Geometry.Should().BeNull();
    }

    [Fact]
    public async Task UpdateRunwayGeometriesAsync_DoesNotCreateNewRunways()
    {
        // Arrange — empty local DB
        SetupAirportLookupResponse(new[]
        {
            new { GLOBAL_ID = "guid-1", IDENT = "DFW" }
        });
        SetupRunwayResponse(new[]
        {
            new
            {
                OBJECTID = 1,
                AIRPORT_ID = "guid-1",
                DESIGNATOR = "17L/35R"
            }
        }, withGeometry: true);

        // Act
        await _service.UpdateRunwayGeometriesAsync();

        // Assert
        var runways = await _dbContext.Runways.ToListAsync();
        runways.Should().BeEmpty();
    }

    #region Helpers

    private void SeedLocalData()
    {
        _dbContext.Airports.Add(new Airport
        {
            SiteNo = "50078.*A",
            ArptId = "DFW",
            IcaoId = "KDFW",
            ArptName = "DALLAS-FT WORTH INTL",
            StateCode = "TX"
        });

        _dbContext.Runways.Add(new Runway
        {
            Id = Guid.NewGuid(),
            SiteNo = "50078.*A",
            RunwayId = "17L/35R",
            Length = 13401,
            Width = 200,
            SurfaceTypeCode = "CONC"
        });

        _dbContext.SaveChanges();
    }

    private void SetupAirportLookupResponse(object[] airports)
    {
        var features = airports.Select(a =>
        {
            var json = JsonSerializer.Serialize(a);
            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json)!;
            return new { attributes = dict };
        }).ToArray();

        var response = JsonSerializer.Serialize(new
        {
            features,
            exceededTransferLimit = false
        });

        _mockHttp.When(HttpMethod.Get, "https://services6.arcgis.com/ssFJjBXIUyZDrSYZ/arcgis/rest/services/US_Airport/FeatureServer/0/query*")
            .Respond("application/json", response);
    }

    private void SetupRunwayResponse(object[] runwayFeatures, bool withGeometry)
    {
        var features = runwayFeatures.Select(r =>
        {
            var json = JsonSerializer.Serialize(r);
            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json)!;
            var feature = new Dictionary<string, object>
            {
                ["attributes"] = dict
            };

            if (withGeometry)
            {
                feature["geometry"] = new
                {
                    rings = new[]
                    {
                        new[]
                        {
                            new[] { -97.04, 32.89 },
                            new[] { -97.03, 32.89 },
                            new[] { -97.03, 32.88 },
                            new[] { -97.04, 32.88 },
                            new[] { -97.04, 32.89 }
                        }
                    }
                };
            }

            return feature;
        }).ToArray();

        var response = JsonSerializer.Serialize(new
        {
            features,
            exceededTransferLimit = false
        });

        _mockHttp.When(HttpMethod.Get, "https://services6.arcgis.com/ssFJjBXIUyZDrSYZ/arcgis/rest/services/Runways/FeatureServer/0/query*")
            .Respond("application/json", response);
    }

    private void SetupRunwayResponseRaw(string json)
    {
        _mockHttp.When(HttpMethod.Get, "https://services6.arcgis.com/ssFJjBXIUyZDrSYZ/arcgis/rest/services/Runways/FeatureServer/0/query*")
            .Respond("application/json", json);
    }

    #endregion
}

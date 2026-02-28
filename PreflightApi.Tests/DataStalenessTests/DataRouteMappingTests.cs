using FluentAssertions;
using PreflightApi.API.Middleware;
using PreflightApi.Domain.Constants;
using Xunit;

namespace PreflightApi.Tests.DataStalenessTests;

public class DataRouteMappingTests
{
    #region ExtractDataRouteSegment

    [Fact]
    public void ExtractSegment_NullPath_ReturnsNull()
    {
        DataRouteMapping.ExtractDataRouteSegment(null).Should().BeNull();
    }

    [Fact]
    public void ExtractSegment_NonApiPath_ReturnsNull()
    {
        DataRouteMapping.ExtractDataRouteSegment("/health/live").Should().BeNull();
    }

    [Fact]
    public void ExtractSegment_TooShortPath_ReturnsNull()
    {
        // Only 2 parts: "api" and "v1"
        DataRouteMapping.ExtractDataRouteSegment("/api/v1").Should().BeNull();
    }

    [Fact]
    public void ExtractSegment_ValidMetarsPath_ReturnsMetars()
    {
        DataRouteMapping.ExtractDataRouteSegment("/api/v1/metars/KDFW").Should().Be("metars");
    }

    [Fact]
    public void ExtractSegment_ValidAirspacesPath_ReturnsAirspaces()
    {
        DataRouteMapping.ExtractDataRouteSegment("/api/v1/airspaces").Should().Be("airspaces");
    }

    [Fact]
    public void ExtractSegment_CaseInsensitiveApiPrefix()
    {
        DataRouteMapping.ExtractDataRouteSegment("/API/V1/metars").Should().Be("metars");
    }

    #endregion

    #region RouteToSyncTypes Dictionary

    [Fact]
    public void RouteToSyncTypes_ContainsAll14DataRoutes()
    {
        DataRouteMapping.RouteToSyncTypes.Should().HaveCount(14);
    }

    [Fact]
    public void RouteToSyncTypes_CaseInsensitiveLookup()
    {
        DataRouteMapping.RouteToSyncTypes.ContainsKey("METARS").Should().BeTrue();
        DataRouteMapping.RouteToSyncTypes.ContainsKey("Metars").Should().BeTrue();
        DataRouteMapping.RouteToSyncTypes.ContainsKey("metars").Should().BeTrue();
    }

    [Fact]
    public void RouteToSyncTypes_AirspacesMapsToBothTypes()
    {
        var types = DataRouteMapping.RouteToSyncTypes["airspaces"];
        types.Should().HaveCount(2);
        types.Should().Contain(SyncTypes.Airspace);
        types.Should().Contain(SyncTypes.SpecialUseAirspace);
    }

    [Fact]
    public void RouteToSyncTypes_RunwaysMapsToAirportAndRunwayGeometry()
    {
        var types = DataRouteMapping.RouteToSyncTypes["runways"];
        types.Should().HaveCount(2);
        types.Should().Contain(SyncTypes.Airport);
        types.Should().Contain(SyncTypes.RunwayGeometry);
    }

    [Theory]
    [InlineData("metars", SyncTypes.Metar)]
    [InlineData("tafs", SyncTypes.Taf)]
    [InlineData("pireps", SyncTypes.Pirep)]
    [InlineData("sigmets", SyncTypes.Sigmet)]
    [InlineData("g-airmets", SyncTypes.GAirmet)]
    [InlineData("notams", SyncTypes.NotamDelta)]
    [InlineData("airports", SyncTypes.Airport)]
    [InlineData("communication-frequencies", SyncTypes.Frequency)]
    [InlineData("obstacles", SyncTypes.Obstacle)]
    [InlineData("chart-supplements", SyncTypes.ChartSupplement)]
    [InlineData("terminal-procedures", SyncTypes.TerminalProcedure)]
    [InlineData("navaids", SyncTypes.Navaid)]
    public void RouteToSyncTypes_EachSingleRouteMapsSingleType(string route, string expectedType)
    {
        var types = DataRouteMapping.RouteToSyncTypes[route];
        types.Should().ContainSingle().Which.Should().Be(expectedType);
    }

    #endregion
}

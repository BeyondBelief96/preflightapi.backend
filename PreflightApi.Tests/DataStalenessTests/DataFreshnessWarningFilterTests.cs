using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PreflightApi.API.Filters;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;
using Xunit;

namespace PreflightApi.Tests.DataStalenessTests;

public class DataFreshnessWarningFilterTests
{
    private readonly DataFreshnessWarningFilter _filter;
    private readonly IDataSyncStatusService _syncService;
    private readonly IMemoryCache _cache;

    public DataFreshnessWarningFilterTests()
    {
        _filter = new DataFreshnessWarningFilter();
        _syncService = Substitute.For<IDataSyncStatusService>();
        _cache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
    }

    #region Helpers

    private (ResultExecutingContext context, bool[] nextCalled) CreateContext(
        string path,
        object? resultValue = null,
        int? statusCode = 200,
        string? acceptWarnings = null)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = path;
        if (acceptWarnings != null)
            httpContext.Request.Headers["Accept-Warnings"] = acceptWarnings;

        // Set up service provider
        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(IMemoryCache)).Returns(_cache);
        sp.GetService(typeof(IDataSyncStatusService)).Returns(_syncService);
        httpContext.RequestServices = sp;

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var objectResult = new ObjectResult(resultValue ?? new { icaoId = "KDFW" })
        {
            StatusCode = statusCode
        };

        var context = new ResultExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            objectResult,
            controller: new object());

        var nextCalled = new[] { false };
        return (context, nextCalled);
    }

    private ResultExecutionDelegate CreateNext(ResultExecutingContext ctx, bool[] nextCalled)
    {
        return () =>
        {
            nextCalled[0] = true;
            return Task.FromResult(new ResultExecutedContext(
                ctx,
                new List<IFilterMetadata>(),
                ctx.Result,
                new object()));
        };
    }

    private void SetupFreshness(params DataFreshnessResult[] results)
    {
        _syncService.GetAllFreshnessAsync(Arg.Any<CancellationToken>())
            .Returns(results.ToList().AsReadOnly());
    }

    private static DataFreshnessResult MakeResult(string syncType, bool isFresh, string severity = "none")
    {
        return new DataFreshnessResult
        {
            SyncType = syncType,
            IsFresh = isFresh,
            Severity = severity,
            StalenessMode = "TimeBased",
            Message = $"{syncType} test message",
            LastSuccessfulSync = DateTime.UtcNow
        };
    }

    #endregion

    #region Header Opt-in

    [Fact]
    public async Task OnResultExecution_NoAcceptWarningsHeader_DoesNotWrap()
    {
        // Arrange — no Accept-Warnings header
        var (ctx, nextCalled) = CreateContext("/api/v1/metars/KDFW");
        SetupFreshness(MakeResult("Metar", isFresh: false, severity: "warning"));

        // Act
        await _filter.OnResultExecutionAsync(ctx, CreateNext(ctx, nextCalled));

        // Assert
        nextCalled[0].Should().BeTrue();
        var result = ctx.Result as ObjectResult;
        result!.Value.Should().NotBeNull();
        result.Value!.GetType().GetProperty("warnings").Should().BeNull();
    }

    [Fact]
    public async Task OnResultExecution_WrongHeaderValue_DoesNotWrap()
    {
        // Arrange
        var (ctx, nextCalled) = CreateContext("/api/v1/metars/KDFW", acceptWarnings: "other");
        SetupFreshness(MakeResult("Metar", isFresh: false, severity: "warning"));

        // Act
        await _filter.OnResultExecutionAsync(ctx, CreateNext(ctx, nextCalled));

        // Assert
        result_ShouldNotHaveWarnings(ctx);
    }

    [Fact]
    public async Task OnResultExecution_CorrectHeader_CaseInsensitive_Wraps()
    {
        // Arrange — "Stale-Data" (mixed case)
        var (ctx, nextCalled) = CreateContext("/api/v1/metars/KDFW", acceptWarnings: "Stale-Data");
        SetupFreshness(MakeResult("Metar", isFresh: false, severity: "warning"));

        // Act
        await _filter.OnResultExecutionAsync(ctx, CreateNext(ctx, nextCalled));

        // Assert
        result_ShouldHaveWarnings(ctx);
    }

    #endregion

    #region Response Type

    [Fact]
    public async Task OnResultExecution_NonObjectResult_DoesNotWrap()
    {
        // Arrange — use ContentResult instead of ObjectResult
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/v1/metars/KDFW";
        httpContext.Request.Headers["Accept-Warnings"] = "stale-data";

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var contentResult = new ContentResult { Content = "text", StatusCode = 200 };
        var context = new ResultExecutingContext(
            actionContext, new List<IFilterMetadata>(), contentResult, new object());

        var nextCalled = false;
        ResultExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new ResultExecutedContext(
                context, new List<IFilterMetadata>(), contentResult, new object()));
        };

        // Act
        await _filter.OnResultExecutionAsync(context, next);

        // Assert
        nextCalled.Should().BeTrue();
        context.Result.Should().BeOfType<ContentResult>();
    }

    [Fact]
    public async Task OnResultExecution_ErrorStatusCode_DoesNotWrap()
    {
        // Arrange — 404 status code
        var (ctx, nextCalled) = CreateContext("/api/v1/metars/KDFW",
            statusCode: 404, acceptWarnings: "stale-data");

        // Act
        await _filter.OnResultExecutionAsync(ctx, CreateNext(ctx, nextCalled));

        // Assert
        result_ShouldNotHaveWarnings(ctx);
    }

    [Fact]
    public async Task OnResultExecution_NullStatusCode_TreatedAs2xx_Wraps()
    {
        // Arrange — null status code is treated as 2xx by the filter
        var (ctx, nextCalled) = CreateContext("/api/v1/metars/KDFW",
            statusCode: null, acceptWarnings: "stale-data");
        SetupFreshness(MakeResult("Metar", isFresh: false, severity: "warning"));

        // Act
        await _filter.OnResultExecutionAsync(ctx, CreateNext(ctx, nextCalled));

        // Assert
        result_ShouldHaveWarnings(ctx);
    }

    #endregion

    #region Route Matching

    [Fact]
    public async Task OnResultExecution_DataRoute_MatchesSyncTypes()
    {
        // Arrange
        var (ctx, nextCalled) = CreateContext("/api/v1/metars/KDFW", acceptWarnings: "stale-data");
        SetupFreshness(MakeResult("Metar", isFresh: false, severity: "warning"));

        // Act
        await _filter.OnResultExecutionAsync(ctx, CreateNext(ctx, nextCalled));

        // Assert
        result_ShouldHaveWarnings(ctx);
    }

    [Fact]
    public async Task OnResultExecution_NonDataRoute_DoesNotWrap()
    {
        // Arrange — e6b routes are not data routes
        var (ctx, nextCalled) = CreateContext("/api/v1/e6b/wind-triangle", acceptWarnings: "stale-data");

        // Act
        await _filter.OnResultExecutionAsync(ctx, CreateNext(ctx, nextCalled));

        // Assert
        result_ShouldNotHaveWarnings(ctx);
    }

    [Fact]
    public async Task OnResultExecution_AirspacesRoute_MapsToMultipleTypes()
    {
        // Arrange — airspaces maps to both Airspace and SpecialUseAirspace
        var (ctx, nextCalled) = CreateContext("/api/v1/airspaces/123", acceptWarnings: "stale-data");
        SetupFreshness(
            MakeResult("Airspace", isFresh: true),
            MakeResult("SpecialUseAirspace", isFresh: false, severity: "warning"));

        // Act
        await _filter.OnResultExecutionAsync(ctx, CreateNext(ctx, nextCalled));

        // Assert — should wrap because SpecialUseAirspace is stale
        result_ShouldHaveWarnings(ctx);
    }

    #endregion

    #region Wrapping Logic

    [Fact]
    public async Task OnResultExecution_StaleData_WrapsResponseWithWarnings()
    {
        // Arrange
        var originalData = new { icaoId = "KDFW" };
        var (ctx, nextCalled) = CreateContext("/api/v1/metars/KDFW",
            resultValue: originalData, acceptWarnings: "stale-data");
        SetupFreshness(MakeResult("Metar", isFresh: false, severity: "warning"));

        // Act
        await _filter.OnResultExecutionAsync(ctx, CreateNext(ctx, nextCalled));

        // Assert
        var result = ctx.Result as ObjectResult;
        var value = result!.Value!;
        var dataProperty = value.GetType().GetProperty("data");
        dataProperty.Should().NotBeNull();
        var warningsProperty = value.GetType().GetProperty("warnings");
        warningsProperty.Should().NotBeNull();
    }

    [Fact]
    public async Task OnResultExecution_AllFresh_DoesNotWrap()
    {
        // Arrange — all relevant types are fresh
        var (ctx, nextCalled) = CreateContext("/api/v1/metars/KDFW", acceptWarnings: "stale-data");
        SetupFreshness(MakeResult("Metar", isFresh: true));

        // Act
        await _filter.OnResultExecutionAsync(ctx, CreateNext(ctx, nextCalled));

        // Assert
        result_ShouldNotHaveWarnings(ctx);
    }

    [Fact]
    public async Task OnResultExecution_ServiceThrows_DoesNotFailRequest()
    {
        // Arrange
        var (ctx, nextCalled) = CreateContext("/api/v1/metars/KDFW", acceptWarnings: "stale-data");
        _syncService.GetAllFreshnessAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("DB error"));

        // Act
        await _filter.OnResultExecutionAsync(ctx, CreateNext(ctx, nextCalled));

        // Assert — request should not fail
        nextCalled[0].Should().BeTrue();
    }

    #endregion

    #region Assertion Helpers

    private static void result_ShouldHaveWarnings(ResultExecutingContext ctx)
    {
        var result = ctx.Result as ObjectResult;
        result.Should().NotBeNull();
        var value = result!.Value!;
        value.GetType().GetProperty("warnings").Should().NotBeNull("response should be wrapped with warnings");
    }

    private static void result_ShouldNotHaveWarnings(ResultExecutingContext ctx)
    {
        var result = ctx.Result as ObjectResult;
        if (result?.Value == null) return;
        var warningsProperty = result.Value.GetType().GetProperty("warnings");
        warningsProperty.Should().BeNull("response should not be wrapped with warnings");
    }

    #endregion
}

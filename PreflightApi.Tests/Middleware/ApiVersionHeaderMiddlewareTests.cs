using FluentAssertions;
using Microsoft.AspNetCore.Http;
using PreflightApi.API.Middleware;
using Xunit;

namespace PreflightApi.Tests.Middleware;

public class ApiVersionHeaderMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldSetXApiVersionHeader()
    {
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ApiVersionHeaderMiddleware(next);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.Headers["X-API-Version"].ToString().Should().NotBeNullOrEmpty();
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNextMiddleware()
    {
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ApiVersionHeaderMiddleware(next);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }
}

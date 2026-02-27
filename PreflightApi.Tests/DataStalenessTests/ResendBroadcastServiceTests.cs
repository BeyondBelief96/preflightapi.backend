using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PreflightApi.Infrastructure.Services;
using PreflightApi.Infrastructure.Settings;
using RichardSzalay.MockHttp;
using Xunit;

namespace PreflightApi.Tests.DataStalenessTests;

public class ResendBroadcastServiceTests
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly ResendSettings _settings;
    private readonly ResendBroadcastService _service;

    public ResendBroadcastServiceTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        _settings = new ResendSettings
        {
            ApiToken = "re_test_token",
            FromAddress = "PreflightAPI <updates@contact.preflightapi.io>",
            ReplyToAddress = "support@preflightapi.io",
            ApiBaseUrl = "https://api.resend.com"
        };

        var httpClient = _mockHttp.ToHttpClient();
        var logger = NSubstitute.Substitute.For<ILogger<ResendBroadcastService>>();

        _service = new ResendBroadcastService(httpClient, Options.Create(_settings), logger);
    }

    #region SendBroadcastAsync

    [Fact]
    public async Task SendBroadcast_PostsCorrectPayload()
    {
        // Arrange
        string? capturedBody = null;
        _mockHttp.When(HttpMethod.Post, "https://api.resend.com/broadcasts")
            .With(req =>
            {
                capturedBody = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return true;
            })
            .Respond("application/json", """{"id": "bc_123"}""");

        // Act
        await _service.SendBroadcastAsync(
            "test-broadcast", "Test Subject", "<h1>Hello</h1>",
            "seg-all-id", "topic-alerts-id");

        // Assert
        capturedBody.Should().NotBeNull();
        using var doc = JsonDocument.Parse(capturedBody!);
        var root = doc.RootElement;

        root.GetProperty("name").GetString().Should().Be("test-broadcast");
        root.GetProperty("segment_id").GetString().Should().Be("seg-all-id");
        root.GetProperty("topic_id").GetString().Should().Be("topic-alerts-id");
        root.GetProperty("from").GetString().Should().Be("PreflightAPI <updates@contact.preflightapi.io>");
        root.GetProperty("reply_to").GetString().Should().Be("support@preflightapi.io");
        root.GetProperty("subject").GetString().Should().Be("Test Subject");
        root.GetProperty("html").GetString().Should().Be("<h1>Hello</h1>");
        root.GetProperty("send").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task SendBroadcast_WithoutTopic_OmitsTopicId()
    {
        // Arrange
        string? capturedBody = null;
        _mockHttp.When(HttpMethod.Post, "https://api.resend.com/broadcasts")
            .With(req =>
            {
                capturedBody = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return true;
            })
            .Respond("application/json", """{"id": "bc_456"}""");

        // Act
        await _service.SendBroadcastAsync(
            "test-broadcast", "Test Subject", "<h1>Hello</h1>",
            "seg-all-id");

        // Assert
        capturedBody.Should().NotBeNull();
        using var doc = JsonDocument.Parse(capturedBody!);
        var root = doc.RootElement;

        root.TryGetProperty("topic_id", out _).Should().BeFalse();
    }

    [Fact]
    public async Task SendBroadcast_ReturnsBroadcastId()
    {
        // Arrange
        _mockHttp.When(HttpMethod.Post, "https://api.resend.com/broadcasts")
            .Respond("application/json", """{"id": "bc_789"}""");

        // Act
        var result = await _service.SendBroadcastAsync(
            "test", "Subject", "<p>Body</p>", "seg-id");

        // Assert
        result.Should().Be("bc_789");
    }

    [Fact]
    public async Task SendBroadcast_NonSuccessStatusCode_ThrowsHttpRequestException()
    {
        // Arrange
        _mockHttp.When(HttpMethod.Post, "https://api.resend.com/broadcasts")
            .Respond(HttpStatusCode.UnprocessableEntity, "application/json",
                """{"message": "Invalid segment ID"}""");

        // Act
        var act = () => _service.SendBroadcastAsync(
            "test", "Subject", "<p>Body</p>", "bad-seg-id");

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*422*Invalid segment ID*");
    }

    [Fact]
    public async Task SendBroadcast_ServerError_ThrowsHttpRequestException()
    {
        // Arrange
        _mockHttp.When(HttpMethod.Post, "https://api.resend.com/broadcasts")
            .Respond(HttpStatusCode.InternalServerError, "application/json",
                """{"message": "Internal error"}""");

        // Act
        var act = () => _service.SendBroadcastAsync(
            "test", "Subject", "<p>Body</p>", "seg-id");

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*500*");
    }

    [Fact]
    public async Task SendBroadcast_CancellationRequested_Throws()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockHttp.When(HttpMethod.Post, "https://api.resend.com/broadcasts")
            .Respond("application/json", """{"id": "bc_123"}""");

        // Act
        var act = () => _service.SendBroadcastAsync(
            "test", "Subject", "<p>Body</p>", "seg-id", ct: cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion
}

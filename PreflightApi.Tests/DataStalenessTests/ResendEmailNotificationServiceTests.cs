using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Services;
using PreflightApi.Infrastructure.Settings;
using Xunit;

namespace PreflightApi.Tests.DataStalenessTests;

public class ResendEmailNotificationServiceTests
{
    private readonly ResendSettings _settings;
    private readonly ILogger<ResendEmailNotificationService> _logger;
    private readonly List<HttpRequestMessage> _capturedRequests = new();

    private const string TestSegmentId = "seg-123";
    private const string TestTopicId = "topic-456";

    public ResendEmailNotificationServiceTests()
    {
        _settings = new ResendSettings
        {
            Enabled = true,
            FromAddress = "alerts@test.io",
            ReplyToAddress = "reply@test.io",
            QuietPeriodMinutes = 60,
            ApiToken = "re_test_token",
            DataAlertsSegmentId = TestSegmentId,
            DataAlertsTopicId = TestTopicId
        };
        _logger = Substitute.For<ILogger<ResendEmailNotificationService>>();
    }

    #region Helpers

    private static List<DataFreshnessResult> MakeStaleTypes(int count)
    {
        return Enumerable.Range(1, count).Select(i => new DataFreshnessResult
        {
            SyncType = $"Type{i}",
            Severity = "warning",
            IsFresh = false,
            StalenessMode = "TimeBased",
            Message = $"Type{i} is stale",
            LastSuccessfulSync = DateTime.UtcNow.AddHours(-i)
        }).ToList();
    }

    private ResendEmailNotificationService CreateService(
        HttpStatusCode responseStatus = HttpStatusCode.OK,
        string responseBody = "{\"id\":\"broadcast_123\"}")
    {
        var handler = new FakeHttpMessageHandler(responseStatus, responseBody, _capturedRequests);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.resend.com")
        };

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("Resend").Returns(httpClient);

        return new ResendEmailNotificationService(
            factory,
            Options.Create(_settings),
            _logger);
    }

    private ResendEmailNotificationService CreateDisabledService()
    {
        var disabledSettings = new ResendSettings
        {
            Enabled = false,
            FromAddress = "alerts@test.io",
            DataAlertsSegmentId = TestSegmentId,
            DataAlertsTopicId = TestTopicId
        };

        var factory = Substitute.For<IHttpClientFactory>();

        return new ResendEmailNotificationService(
            factory,
            Options.Create(disabledSettings),
            _logger);
    }

    private async Task<JsonElement> GetCapturedPayload()
    {
        _capturedRequests.Should().ContainSingle();
        var content = await _capturedRequests[0].Content!.ReadAsStringAsync();
        return JsonDocument.Parse(content).RootElement;
    }

    #endregion

    #region SendStalenessAlertAsync

    [Fact]
    public async Task SendStalenessAlert_Disabled_DoesNotSendBroadcast()
    {
        var service = CreateDisabledService();

        await service.SendStalenessAlertAsync(MakeStaleTypes(2));

        _capturedRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task SendStalenessAlert_SendsSingleBroadcast()
    {
        var service = CreateService();

        await service.SendStalenessAlertAsync(MakeStaleTypes(2));

        _capturedRequests.Should().ContainSingle();
        _capturedRequests[0].Method.Should().Be(HttpMethod.Post);
        _capturedRequests[0].RequestUri!.PathAndQuery.Should().Be("/broadcasts");
    }

    [Fact]
    public async Task SendStalenessAlert_PayloadContainsCorrectSegmentAndTopicIds()
    {
        var service = CreateService();

        await service.SendStalenessAlertAsync(MakeStaleTypes(1));

        var payload = await GetCapturedPayload();
        payload.GetProperty("segment_id").GetString().Should().Be(TestSegmentId);
        payload.GetProperty("topic_id").GetString().Should().Be(TestTopicId);
    }

    [Fact]
    public async Task SendStalenessAlert_PayloadContainsSendTrue()
    {
        var service = CreateService();

        await service.SendStalenessAlertAsync(MakeStaleTypes(1));

        var payload = await GetCapturedPayload();
        payload.GetProperty("send").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task SendStalenessAlert_SubjectContainsCount()
    {
        var service = CreateService();

        await service.SendStalenessAlertAsync(MakeStaleTypes(3));

        var payload = await GetCapturedPayload();
        payload.GetProperty("subject").GetString().Should().Contain("3 type(s) stale");
    }

    [Fact]
    public async Task SendStalenessAlert_SetsFromAndReplyTo()
    {
        var service = CreateService();

        await service.SendStalenessAlertAsync(MakeStaleTypes(1));

        var payload = await GetCapturedPayload();
        payload.GetProperty("from").GetString().Should().Be("alerts@test.io");
        payload.GetProperty("reply_to").GetString().Should().Be("reply@test.io");
    }

    [Fact]
    public async Task SendStalenessAlert_HtmlContainsUnsubscribeUrl()
    {
        var service = CreateService();

        await service.SendStalenessAlertAsync(MakeStaleTypes(1));

        var payload = await GetCapturedPayload();
        payload.GetProperty("html").GetString().Should().Contain("{{{RESEND_UNSUBSCRIBE_URL}}}");
    }

    [Fact]
    public async Task SendStalenessAlert_ApiFailure_ThrowsHttpRequestException()
    {
        var service = CreateService(HttpStatusCode.InternalServerError, "Server error");

        var act = () => service.SendStalenessAlertAsync(MakeStaleTypes(1));

        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*500*Server error*");
    }

    #endregion

    #region SendRecoveryNoticeAsync

    [Fact]
    public async Task SendRecoveryNotice_Disabled_DoesNotSend()
    {
        var service = CreateDisabledService();

        await service.SendRecoveryNoticeAsync(new List<string> { "Metar", "Taf" });

        _capturedRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task SendRecoveryNotice_SendsSingleBroadcast()
    {
        var service = CreateService();

        await service.SendRecoveryNoticeAsync(new List<string> { "Metar" });

        _capturedRequests.Should().ContainSingle();
        _capturedRequests[0].RequestUri!.PathAndQuery.Should().Be("/broadcasts");
    }

    [Fact]
    public async Task SendRecoveryNotice_SubjectContainsTypeNames()
    {
        var service = CreateService();

        await service.SendRecoveryNoticeAsync(new List<string> { "Metar", "Taf" });

        var payload = await GetCapturedPayload();
        var subject = payload.GetProperty("subject").GetString();
        subject.Should().Contain("Metar").And.Contain("Taf");
    }

    [Fact]
    public async Task SendRecoveryNotice_PayloadContainsCorrectSegmentAndTopicIds()
    {
        var service = CreateService();

        await service.SendRecoveryNoticeAsync(new List<string> { "Metar" });

        var payload = await GetCapturedPayload();
        payload.GetProperty("segment_id").GetString().Should().Be(TestSegmentId);
        payload.GetProperty("topic_id").GetString().Should().Be(TestTopicId);
    }

    [Fact]
    public async Task SendRecoveryNotice_HtmlContainsUnsubscribeUrl()
    {
        var service = CreateService();

        await service.SendRecoveryNoticeAsync(new List<string> { "Metar" });

        var payload = await GetCapturedPayload();
        payload.GetProperty("html").GetString().Should().Contain("{{{RESEND_UNSUBSCRIBE_URL}}}");
    }

    [Fact]
    public async Task SendRecoveryNotice_ApiFailure_ThrowsHttpRequestException()
    {
        var service = CreateService(HttpStatusCode.BadRequest, "Invalid request");

        var act = () => service.SendRecoveryNoticeAsync(new List<string> { "Metar" });

        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*400*Invalid request*");
    }

    #endregion

    #region Cancellation

    [Fact]
    public async Task SendStalenessAlert_CancellationRequested_Throws()
    {
        var service = CreateService();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => service.SendStalenessAlertAsync(MakeStaleTypes(1), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task SendRecoveryNotice_CancellationRequested_Throws()
    {
        var service = CreateService();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => service.SendRecoveryNoticeAsync(new List<string> { "Metar" }, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region FakeHttpMessageHandler

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseBody;
        private readonly List<HttpRequestMessage> _captured;

        public FakeHttpMessageHandler(
            HttpStatusCode statusCode,
            string responseBody,
            List<HttpRequestMessage> captured)
        {
            _statusCode = statusCode;
            _responseBody = responseBody;
            _captured = captured;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Clone the content before it's disposed
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);
            if (request.Content != null)
            {
                var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
                clone.Content = new ByteArrayContent(bytes);
                clone.Content.Headers.ContentType = request.Content.Headers.ContentType;
            }
            _captured.Add(clone);

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody)
            };
        }
    }

    #endregion
}

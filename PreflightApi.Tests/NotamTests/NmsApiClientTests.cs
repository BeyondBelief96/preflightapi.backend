using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using RichardSzalay.MockHttp;
using PreflightApi.Infrastructure.Dtos.Notam;
using PreflightApi.Infrastructure.Services.NotamServices;
using PreflightApi.Infrastructure.Settings;
using Xunit;

namespace PreflightApi.Tests.NotamTests;

public class NmsApiClientTests
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<NmsSettings> _settings;
    private readonly ILogger<NmsApiClient> _logger;
    private readonly NmsApiClient _client;

    private const string BaseUrl = "https://api-staging.cgifederal-aim.com/nmsapi";
    private const string AuthBaseUrl = "https://api-staging.cgifederal-aim.com";
    private const string TestClientId = "test-client-id";
    private const string TestClientSecret = "test-client-secret";
    private const string TestAccessToken = "test-access-token-12345";

    public NmsApiClientTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _httpClientFactory.CreateClient("NmsApi").Returns(_ => _mockHttp.ToHttpClient());

        _settings = Options.Create(new NmsSettings
        {
            BaseUrl = BaseUrl,
            AuthBaseUrl = AuthBaseUrl,
            ClientId = TestClientId,
            ClientSecret = TestClientSecret,
            CacheDurationMinutes = 5,
            RequestTimeoutSeconds = 30
        });

        _logger = Substitute.For<ILogger<NmsApiClient>>();

        _client = new NmsApiClient(_httpClientFactory, _settings, _logger);
    }

    [Fact]
    public async Task GetNotamsByLocationAsync_ShouldReturnNotams_WhenApiReturnsValidResponse()
    {
        // Arrange
        SetupTokenEndpoint();
        SetupLocationEndpoint("KDFW", CreateSampleGeoJsonResponse());

        // Act
        var result = await _client.GetNotamsByLocationAsync("KDFW");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Type.Should().Be("Feature");
        result[0].Properties?.CoreNotamData?.Notam?.Location.Should().Be("KDFW");
    }

    [Fact]
    public async Task GetNotamsByLocationAsync_ShouldNormalizeLocationToUppercase()
    {
        // Arrange
        SetupTokenEndpoint();
        _mockHttp.When($"{BaseUrl}/v1/notams?location=KDFW")
            .Respond("application/json", CreateSampleGeoJsonResponse());

        // Act
        var result = await _client.GetNotamsByLocationAsync("kdfw");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetNotamsByLocationAsync_ShouldThrowArgumentException_WhenLocationIsEmpty()
    {
        // Act
        Func<Task> act = async () => await _client.GetNotamsByLocationAsync("");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*cannot be null or empty*");
    }

    [Fact]
    public async Task GetNotamsByRadiusAsync_ShouldReturnNotams_WhenApiReturnsValidResponse()
    {
        // Arrange
        SetupTokenEndpoint();
        _mockHttp.When($"{BaseUrl}/v1/notams*")
            .WithQueryString("latitude", "32.897000")
            .Respond("application/json", CreateSampleGeoJsonResponse());

        // Act
        var result = await _client.GetNotamsByRadiusAsync(32.897, -97.038, 25.0);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetNotamsByRadiusAsync_ShouldThrowArgumentOutOfRangeException_WhenRadiusTooLarge()
    {
        // Act
        Func<Task> act = async () => await _client.GetNotamsByRadiusAsync(32.897, -97.038, 150.0);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*must be between 0 and 100*");
    }

    [Fact]
    public async Task GetNotamsByRadiusAsync_ShouldThrowArgumentOutOfRangeException_WhenRadiusIsZero()
    {
        // Act
        Func<Task> act = async () => await _client.GetNotamsByRadiusAsync(32.897, -97.038, 0);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task GetNotamsByLocationAsync_ShouldRefreshToken_When401Received()
    {
        // Arrange
        var tokenCallCount = 0;
        _mockHttp.When(HttpMethod.Post, $"{AuthBaseUrl}/v1/auth/token")
            .Respond(_ =>
            {
                tokenCallCount++;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CreateTokenResponse(), Encoding.UTF8, "application/json")
                };
            });

        var firstCall = true;
        _mockHttp.When($"{BaseUrl}/v1/notams?location=KDFW")
            .Respond(_ =>
            {
                if (firstCall)
                {
                    firstCall = false;
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                }
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CreateSampleGeoJsonResponse(), Encoding.UTF8, "application/json")
                };
            });

        // Act
        var result = await _client.GetNotamsByLocationAsync("KDFW");

        // Assert
        result.Should().NotBeNull();
        tokenCallCount.Should().Be(2); // Initial token + refresh after 401
    }

    [Fact]
    public async Task GetNotamsByLocationAsync_ShouldReuseToken_WhenNotExpired()
    {
        // Arrange
        var tokenCallCount = 0;
        _mockHttp.When(HttpMethod.Post, $"{AuthBaseUrl}/v1/auth/token")
            .Respond(_ =>
            {
                tokenCallCount++;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CreateTokenResponse(3600), Encoding.UTF8, "application/json")
                };
            });

        _mockHttp.When($"{BaseUrl}/v1/notams*")
            .Respond("application/json", CreateSampleGeoJsonResponse());

        // Act
        await _client.GetNotamsByLocationAsync("KDFW");
        await _client.GetNotamsByLocationAsync("KORD");
        await _client.GetNotamsByLocationAsync("KLAX");

        // Assert
        tokenCallCount.Should().Be(1); // Only one token request
    }

    [Fact]
    public async Task GetNotamsByLocationAsync_ShouldThrow_WhenCredentialsNotConfigured()
    {
        // Arrange
        var settingsNoCredentials = Options.Create(new NmsSettings
        {
            BaseUrl = BaseUrl,
            ClientId = "",
            ClientSecret = ""
        });
        var clientNoCredentials = new NmsApiClient(_httpClientFactory, settingsNoCredentials, _logger);

        // Act
        Func<Task> act = async () => await clientNoCredentials.GetNotamsByLocationAsync("KDFW");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ClientId and ClientSecret must be configured*");
    }

    [Fact]
    public async Task GetNotamsByLocationAsync_ShouldReturnEmptyList_WhenNoNotamsFound()
    {
        // Arrange
        SetupTokenEndpoint();
        var emptyResponse = """{"status": "Success", "data": {"geojson": []}}""";
        _mockHttp.When($"{BaseUrl}/v1/notams*")
            .WithQueryString("location", "KXYZ")
            .Respond("application/json", emptyResponse);

        // Act
        var result = await _client.GetNotamsByLocationAsync("KXYZ");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNotamsByLocationAsync_ShouldParseDataAixmFormat()
    {
        // Arrange
        SetupTokenEndpoint();

        // Build a data.aixm response: JSON with an array of AIXM XML strings
        var aixmXml = """
            <msg:AIXMBasicMessage
            xmlns:msg="http://www.aixm.aero/schema/5.1/message"
            xmlns:aixm="http://www.aixm.aero/schema/5.1"
            xmlns:event="http://www.aixm.aero/schema/5.1/event"
            xmlns:gml="http://www.opengis.net/gml/3.2"
            xmlns:fnse="http://www.aixm.aero/schema/5.1/extensions/FAA/FNSE"
            xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
            gml:id="NMS_ID_AIXM_TEST">
            <gml:boundedBy xsi:nil="true"/>
            <hasMember><event:Event gml:id="Event_AIXM"><gml:identifier codeSpace="urn:uuid:">aixm-test</gml:identifier><gml:boundedBy xsi:nil="true"/><event:timeSlice><event:EventTimeSlice gml:id="Event_AIXM_TS"><gml:validTime><gml:TimePeriod gml:id="Event_AIXM_TP"><gml:beginPosition>2025-01-01T00:00:00Z</gml:beginPosition><gml:endPosition>2025-06-01T00:00:00Z</gml:endPosition></gml:TimePeriod></gml:validTime><aixm:interpretation>BASELINE</aixm:interpretation><aixm:sequenceNumber>1</aixm:sequenceNumber><aixm:correctionNumber>0</aixm:correctionNumber><event:scenario>87</event:scenario><event:textNOTAM><event:NOTAM gml:id="NOTAM_AIXM"><event:number>99</event:number><event:year>2025</event:year><event:type>N</event:type><event:issued>2025-01-01T00:00:00Z</event:issued><event:location>DFW</event:location><event:effectiveStart>202501010000</event:effectiveStart><event:effectiveEnd>202506010000</event:effectiveEnd><event:text>TWY B CLSD</event:text></event:NOTAM></event:textNOTAM><event:extension><fnse:EventExtension gml:id="ext_AIXM"><fnse:classification>DOM</fnse:classification><fnse:accountId>DFW</fnse:accountId><fnse:lastUpdated>2025-01-01T00:00:00Z</fnse:lastUpdated></fnse:EventExtension></event:extension></event:EventTimeSlice></event:timeSlice></event:Event></hasMember>
            </msg:AIXMBasicMessage>
            """;

        var escapedXml = JsonSerializer.Serialize(aixmXml); // JSON-escaped string
        var response = """{"status": "Success", "data": {"aixm": [""" + escapedXml + "]}}";


        SetupLocationEndpoint("KDFW", response);

        // Act
        var result = await _client.GetNotamsByLocationAsync("KDFW");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("NMS_ID_AIXM_TEST");
        result[0].Properties?.CoreNotamData?.Notam?.Number.Should().Be("99");
        result[0].Properties?.CoreNotamData?.Notam?.Location.Should().Be("DFW");
        result[0].Properties?.CoreNotamData?.Notam?.Text.Should().Be("TWY B CLSD");
    }

    private void SetupTokenEndpoint()
    {
        _mockHttp.When(HttpMethod.Post, $"{AuthBaseUrl}/v1/auth/token")
            .Respond("application/json", CreateTokenResponse());
    }

    private void SetupLocationEndpoint(string location, string response)
    {
        _mockHttp.When($"{BaseUrl}/v1/notams?location={location}")
            .Respond("application/json", response);
    }

    private static string CreateTokenResponse(int expiresIn = 3600)
    {
        return JsonSerializer.Serialize(new
        {
            access_token = TestAccessToken,
            issued_at = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            expires_in = expiresIn
        });
    }

    private static string CreateSampleGeoJsonResponse()
    {
        return """
        {
            "status": "Success",
            "data": {
                "geojson": [
                    {
                        "type": "Feature",
                        "id": "NOTAM-123",
                        "geometry": {
                            "type": "Point",
                            "coordinates": [-97.038, 32.897]
                        },
                        "properties": {
                            "coreNOTAMData": {
                                "notam": {
                                    "id": "NOTAM-123",
                                    "number": "01/001",
                                    "series": "A",
                                    "type": "N",
                                    "issued": "2024-01-15T12:00:00Z",
                                    "effectiveStart": "2024-01-15T12:00:00Z",
                                    "effectiveEnd": "2024-02-15T12:00:00Z",
                                    "location": "KDFW",
                                    "text": "RWY 18L/36R CLSD"
                                },
                                "notamTranslation": [
                                    {
                                        "type": "LOCAL_FORMAT",
                                        "simpleText": "Runway 18L/36R closed"
                                    }
                                ]
                            }
                        }
                    }
                ]
            }
        }
        """;
    }
}

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PreflightApi.Tests.EmailVerificationTests
{
    public sealed class ResendDirectEmailSender : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _from;
        private readonly string _to;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public ResendDirectEmailSender(string apiToken, string fromAddress, string toAddress)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiToken);
            _from = fromAddress;
            _to = toAddress;
        }

        public async Task SendAsync(string subject, string html)
        {
            var payload = new
            {
                from = _from,
                to = new[] { _to },
                subject,
                html
            };

            var response = await _httpClient.PostAsJsonAsync(
                "https://api.resend.com/emails", payload, JsonOptions);

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Resend direct email API returned {(int)response.StatusCode}: {responseBody}");
            }
        }

        public void Dispose() => _httpClient.Dispose();
    }
}

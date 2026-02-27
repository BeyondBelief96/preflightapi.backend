using System.Net.Http.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Settings;

namespace PreflightApi.Azure.Functions.Functions;

public class ServiceOutageAlertFunction
{
    private readonly HttpClient _httpClient;
    private readonly IServiceHealthAlertStateService _alertStateService;
    private readonly IEmailNotificationService _emailService;
    private readonly ResendSettings _settings;
    private readonly ILogger _logger;

    private static readonly Dictionary<string, int> SeverityRanks = new()
    {
        ["none"] = 0,
        ["degraded"] = 1,
        ["unhealthy"] = 2
    };

    public ServiceOutageAlertFunction(
        IHttpClientFactory httpClientFactory,
        IServiceHealthAlertStateService alertStateService,
        IEmailNotificationService emailService,
        IOptions<ResendSettings> settings,
        ILoggerFactory loggerFactory)
    {
        _httpClient = httpClientFactory.CreateClient("HealthEndpoint");
        _alertStateService = alertStateService;
        _emailService = emailService;
        _settings = settings.Value;
        _logger = loggerFactory.CreateLogger<ServiceOutageAlertFunction>();
    }

    [Function("ServiceOutageAlertFunction")]
    [ExponentialBackoffRetry(3, "00:00:30", "00:05:00")]
    public async Task Run(
        [TimerTrigger("0 */5 * * * *", RunOnStartup = false)] TimerInfo myTimer,
        FunctionContext context)
    {
        _logger.LogInformation("ServiceOutageAlertFunction executed at: {Time}", DateTime.UtcNow);

        var ct = context.CancellationToken;
        var healthChecks = await FetchHealthChecksAsync(ct);
        var priorStates = await _alertStateService.GetAllAsync(ct);
        var priorStateMap = priorStates.ToDictionary(s => s.ServiceName);
        var now = DateTime.UtcNow;
        var quietPeriod = TimeSpan.FromMinutes(_settings.QuietPeriodMinutes);

        var servicesNeedingAlert = new List<HealthCheckEntry>();
        var recoveredServices = new List<string>();

        foreach (var check in healthChecks)
        {
            var severity = MapStatusToSeverity(check.Status);
            var isHealthy = severity == "none";

            // Upsert the current status in DB
            await _alertStateService.UpsertStatusAsync(check.Name, check.Status, ct);

            priorStateMap.TryGetValue(check.Name, out var prior);

            if (!isHealthy)
            {
                var shouldAlert =
                    prior?.LastAlertSentUtc == null
                    || SeverityEscalated(prior.LastAlertSeverity, severity)
                    || (now - prior.LastAlertSentUtc.Value) > quietPeriod;

                if (shouldAlert)
                    servicesNeedingAlert.Add(check);
            }
            else if (prior?.LastAlertSeverity != null)
            {
                // Was alerted before and is now healthy → recovery
                recoveredServices.Add(check.Name);
            }
        }

        // Send service outage alerts
        if (servicesNeedingAlert.Count > 0)
        {
            _logger.LogInformation("Sending service outage alert for {Count} service(s): {Services}",
                servicesNeedingAlert.Count,
                string.Join(", ", servicesNeedingAlert.Select(s => s.Name)));

            await _emailService.SendServiceOutageAlertAsync(servicesNeedingAlert, ct);

            foreach (var service in servicesNeedingAlert)
            {
                var severity = MapStatusToSeverity(service.Status);
                await _alertStateService.UpdateAlertStateAsync(service.Name, severity, ct);
            }
        }

        // Send recovery notices
        if (recoveredServices.Count > 0)
        {
            _logger.LogInformation("Sending service recovery notice for {Count} service(s): {Services}",
                recoveredServices.Count,
                string.Join(", ", recoveredServices));

            await _emailService.SendServiceRecoveryNoticeAsync(recoveredServices, ct);

            foreach (var service in recoveredServices)
            {
                await _alertStateService.ClearAlertStateAsync(service, ct);
            }
        }

        if (servicesNeedingAlert.Count == 0 && recoveredServices.Count == 0)
        {
            _logger.LogInformation("No service alerting or recovery actions needed");
        }
    }

    internal async Task<IReadOnlyList<HealthCheckEntry>> FetchHealthChecksAsync(CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync(_settings.HealthEndpointUrl, ct);
            response.EnsureSuccessStatusCode();

            var healthResponse = await response.Content.ReadFromJsonAsync<HealthCheckResponse>(ct);
            if (healthResponse?.Checks == null)
                return [];

            return healthResponse.Checks.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch health checks from {Url} — treating API as unhealthy",
                _settings.HealthEndpointUrl);

            return new List<HealthCheckEntry>
            {
                new()
                {
                    Name = "api",
                    Status = "Unhealthy",
                    Description = $"Health endpoint unreachable: {ex.Message}"
                }
            };
        }
    }

    private static string MapStatusToSeverity(string status) => status switch
    {
        "Healthy" => "none",
        "Degraded" => "degraded",
        "Unhealthy" => "unhealthy",
        _ => "unhealthy"
    };

    private static bool SeverityEscalated(string? previousSeverity, string currentSeverity)
    {
        if (previousSeverity == null) return true;

        SeverityRanks.TryGetValue(previousSeverity, out var prevRank);
        SeverityRanks.TryGetValue(currentSeverity, out var currRank);

        return currRank > prevRank;
    }
}

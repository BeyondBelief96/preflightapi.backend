using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Services.CloudStorage;
using PreflightApi.Infrastructure.Settings;

namespace PreflightApi.Infrastructure.Utilities;

public static class ServiceCollectionExtensions
{
    public const string WeatherHttpClient = "Weather";
    public const string FaaDataHttpClient = "FaaData";
    public const string MagVarHttpClient = "MagVar";

    /// <summary>
    /// Registers Azure Blob Storage services for cloud storage.
    /// </summary>
    public static IServiceCollection AddCloudStorageServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Azure Blob Storage settings
        services.Configure<CloudStorageSettings>(configuration.GetSection("CloudStorage"));

        // Register Azure Blob Storage implementation
        services.AddScoped<ICloudStorageService, AzureBlobStorageService>();

        // Register initialization service
        services.AddScoped<ICloudStorageInitializationService, CloudStorageInitializationService>();

        return services;
    }

    /// <summary>
    /// Registers named HttpClients with Polly retry and circuit breaker policies for external service resilience.
    /// Weather client: NOAA Aviation Weather cache endpoints.
    /// FaaData client: FAA NASR data, obstacles, diagrams, chart supplements (large downloads, 5-min timeout).
    /// MagVar client: NOAA Geomagnetic Web API for magnetic declination.
    /// </summary>
    public static IServiceCollection AddResilientHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient(WeatherHttpClient)
            .AddPolicyHandler(CreateRetryPolicy())
            .AddPolicyHandler(CreateCircuitBreakerPolicy());

        services.AddHttpClient(FaaDataHttpClient, client =>
        {
            client.Timeout = TimeSpan.FromMinutes(5);
        })
        .AddPolicyHandler(CreateRetryPolicy())
        .AddPolicyHandler(CreateCircuitBreakerPolicy());

        services.AddHttpClient(MagVarHttpClient)
            .AddPolicyHandler(CreateRetryPolicy())
            .AddPolicyHandler(CreateCircuitBreakerPolicy());

        return services;
    }

    public static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<HttpIOException>()
            .Or<TaskCanceledException>()
            .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    public static IAsyncPolicy<HttpResponseMessage> CreateCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<HttpIOException>()
            .Or<TaskCanceledException>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));
    }
}

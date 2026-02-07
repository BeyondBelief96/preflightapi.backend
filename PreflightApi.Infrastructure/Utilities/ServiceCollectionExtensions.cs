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
    /// Registers named HttpClients with Polly retry policies for external service resilience.
    /// Weather client: NOAA Aviation Weather cache endpoints.
    /// FaaData client: FAA NASR data, obstacles, diagrams, chart supplements (large downloads, 5-min timeout).
    /// </summary>
    public static IServiceCollection AddResilientHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient(WeatherHttpClient)
            .AddPolicyHandler(CreateRetryPolicy());

        services.AddHttpClient(FaaDataHttpClient, client =>
        {
            client.Timeout = TimeSpan.FromMinutes(5);
        })
        .AddPolicyHandler(CreateRetryPolicy());

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy()
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
}

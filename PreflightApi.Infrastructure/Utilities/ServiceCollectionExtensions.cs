using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Services.CloudStorage;
using PreflightApi.Infrastructure.Settings;

namespace PreflightApi.Infrastructure.Utilities;

public static class ServiceCollectionExtensions
{
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
}

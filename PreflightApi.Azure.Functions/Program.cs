using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Services;
using PreflightApi.Infrastructure.Services.CertificateRenewal;
using PreflightApi.Infrastructure.Services.CronJobServices;
using PreflightApi.Infrastructure.Services.CronJobServices.ArcGisServices;
using PreflightApi.Infrastructure.Services.CronJobServices.NasrServices;
using PreflightApi.Infrastructure.Services.NotamServices;
using PreflightApi.Infrastructure.Services.Telemetry;
using PreflightApi.Infrastructure.Settings;
using PreflightApi.Infrastructure.Utilities;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Configuration
    .SetBasePath(Path.GetDirectoryName(typeof(Program).Assembly.Location)!)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true, reloadOnChange: true);

// Add Application Insights early - before other service registrations
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// ConfigureFunctionsApplicationInsights() adds a filter that defaults the Application Insights
// logger to Warning level, which silently drops all Information/Debug logs. Remove that filter
// so our host.json log level configuration controls what gets captured.
// See: https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#application-insights
builder.Logging.Services.Configure<LoggerFilterOptions>(options =>
{
    var toRemove = options.Rules.FirstOrDefault(rule =>
        rule.ProviderName == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
    if (toRemove is not null)
        options.Rules.Remove(toRemove);
});

// Register settings
builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("Database"));
builder.Services.Configure<NmsSettings>(builder.Configuration.GetSection("NmsSettings"));
builder.Services.Configure<PorkbunSettings>(builder.Configuration.GetSection("Porkbun"));
builder.Services.Configure<CertificateRenewalSettings>(builder.Configuration.GetSection("CertificateRenewal"));

// Register services
builder.Services.AddScoped<IFaaPublicationCycleService, FaaPublicationCycleService>();
builder.Services.AddScoped<IChartSupplementCronService, ChartSupplementCronService>();
builder.Services.AddScoped<IAirportDiagramCronService, AirportDiagramCronService>();
builder.Services.AddScoped<IAviationWeatherService<Metar>, MetarCronService>();
builder.Services.AddScoped<IAviationWeatherService<Taf>, TafCronService>();
builder.Services.AddScoped<IAviationWeatherService<Sigmet>, SigmetCronService>();
builder.Services.AddScoped<IAviationWeatherService<GAirmet>, GAirmetCronService>();
builder.Services.AddScoped<IAviationWeatherService<Pirep>, PirepCronService>();
builder.Services.AddScoped<IAirspaceCronService<Airspace>, AirspaceCronService>();
builder.Services.AddScoped<IAirspaceCronService<SpecialUseAirspace>, SpecialUseAirspaceCronService>();
builder.Services.AddScoped<IAirportCronService, AirportCronService>();
builder.Services.AddScoped<ICommunicationFrequencyCronService, CommunicationFrequencyCronService>();
builder.Services.AddScoped<IRunwayCronService, RunwayCronService>();
builder.Services.AddScoped<IRunwayEndCronService, RunwayEndCronService>();
builder.Services.AddScoped<IObstacleCronService, ObstacleCronService>();
builder.Services.AddScoped<IObstacleDailyChangeCronService, ObstacleDailyChangeCronService>();
builder.Services.AddSingleton<INmsApiClient, NmsApiClient>();
builder.Services.AddScoped<INotamDeltaSyncCronService, NotamDeltaSyncCronService>();
builder.Services.AddScoped<INotamInitialLoadCronService, NotamInitialLoadCronService>();
builder.Services.AddScoped<IPorkbunDnsClient, PorkbunDnsClient>();
builder.Services.AddScoped<IKeyVaultCertificateService, KeyVaultCertificateService>();
builder.Services.AddScoped<ICertificateRenewalService, CertificateRenewalService>();
builder.Services.AddSingleton<ISyncTelemetryService, SyncTelemetryService>();
builder.Services.AddCloudStorageServices(builder.Configuration);
builder.Services.AddResilientHttpClients();

// Configure HttpClient for ArcGIS services with extended timeout (has its own Polly retry in ArcGisBaseService)
builder.Services.AddHttpClient("ArcGis", client =>
{
    client.Timeout = TimeSpan.FromMinutes(10);
});

// Configure HttpClient for NMS API with configurable timeout + retry and circuit breaker
builder.Services.AddHttpClient("NmsApi", (serviceProvider, client) =>
{
    var nmsSettings = serviceProvider.GetRequiredService<IOptions<NmsSettings>>().Value;
    client.Timeout = TimeSpan.FromSeconds(nmsSettings.RequestTimeoutSeconds);
})
.AddPolicyHandler(PreflightApi.Infrastructure.Utilities.ServiceCollectionExtensions.CreateRetryPolicy())
.AddPolicyHandler(PreflightApi.Infrastructure.Utilities.ServiceCollectionExtensions.CreateCircuitBreakerPolicy());

// Configure HttpClient for Porkbun DNS API
builder.Services.AddHttpClient("Porkbun");

// Register database context
builder.Services.AddDbContext<PreflightApiDbContext>((serviceProvider, options) =>
{
    var dbSettings = serviceProvider.GetRequiredService<IOptions<DatabaseSettings>>().Value;

    var connectionString = dbSettings.GetConnectionString();

    options.UseNpgsql(connectionString,
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(3);
            npgsqlOptions.CommandTimeout(300); // 5 minutes for heavy NASR data operations
            npgsqlOptions.UseNetTopologySuite();
        });

    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging();
    }
});

// Build the application
var app = builder.Build();

// Get logger from root provider (singletons are fine)
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Initialize Azure Blob Storage resources on startup (scoped service - needs scope)
logger.LogInformation("Initializing Azure Blob Storage resources during startup...");
try
{
    using (var scope = app.Services.CreateScope())
    {
        var cloudStorageInitService = scope.ServiceProvider.GetRequiredService<ICloudStorageInitializationService>();
        cloudStorageInitService.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
    }
    logger.LogInformation("Azure Blob Storage resources initialized successfully");
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to initialize Azure Blob Storage resources");
}

// Database seeding (scoped service - needs scope)
logger.LogInformation("Initializing database data...");
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<PreflightApiDbContext>();
        DbInitializer.InitializeAsync(dbContext, logger).GetAwaiter().GetResult();
    }
    logger.LogInformation("Database initialization completed successfully");
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to initialize database data");
}

app.Run();
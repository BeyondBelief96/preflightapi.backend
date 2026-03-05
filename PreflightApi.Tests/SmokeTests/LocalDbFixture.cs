using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Settings;
using Xunit;

namespace PreflightApi.Tests.SmokeTests;

public class LocalDbFixture : IAsyncLifetime
{
    public PreflightApiDbContext DbContext { get; private set; } = null!;
    public bool IsAvailable { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(FindApiProjectPath())
                .AddJsonFile("appsettings.Development.json", optional: false)
                .AddEnvironmentVariables()
                .Build();

            var dbSettings = new DatabaseSettings();
            config.GetSection("Database").Bind(dbSettings);

            // Allow password from env var (same as docker-compose)
            if (string.IsNullOrEmpty(dbSettings.Password))
                dbSettings.Password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";

            var connectionString = dbSettings.GetConnectionString();

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.UseNetTopologySuite();
            dataSourceBuilder.EnableDynamicJson();
            var dataSource = dataSourceBuilder.Build();

            var options = new DbContextOptionsBuilder<PreflightApiDbContext>()
                .UseNpgsql(dataSource, o => o.UseNetTopologySuite())
                .Options;

            DbContext = new PreflightApiDbContext(options);

            // Verify connectivity
            IsAvailable = await DbContext.Database.CanConnectAsync();
        }
        catch
        {
            IsAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (DbContext != null)
            await DbContext.DisposeAsync();
    }

    private static string FindApiProjectPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "PreflightApi.API")))
            dir = dir.Parent;

        return dir != null
            ? Path.Combine(dir.FullName, "PreflightApi.API")
            : throw new DirectoryNotFoundException("Could not find PreflightApi.API project directory");
    }
}

[CollectionDefinition("LocalDb")]
public class LocalDbCollection : ICollectionFixture<LocalDbFixture>;

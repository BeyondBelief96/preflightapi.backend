extern alias api;

using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using NSubstitute;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Interfaces;
using Testcontainers.PostgreSql;
using Xunit;

namespace PreflightApi.Tests.IntegrationTests.ApiKeyAuth;

/// <summary>
/// Spins up a Postgres container + the API in-process via WebApplicationFactory so
/// integration tests can drive the full middleware pipeline (auth, tier gating,
/// quota, rate limiter, output cache) over in-memory HTTP.
/// </summary>
public class ApiKeyTestFixture : WebApplicationFactory<api::Program>, IAsyncLifetime
{
    private const string Base62Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const string KeyPrefix = "pfa_sk_";

    private readonly PostgreSqlContainer _container;

    public ApiKeyTestFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgis/postgis:15-3.3")
            .WithDatabase("preflightapi_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .WithAutoRemove(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Touching .Server forces the host to build with our overrides applied,
        // so the schema + seeded keys are written into the right DbContext instance.
        _ = Server;

        using var scope = Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<PreflightApiDbContext>();
        await ctx.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        await _container.StopAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Host"]     = _container.Hostname,
                ["Database:Port"]     = _container.GetMappedPublicPort(5432).ToString(),
                ["Database:Database"] = "preflightapi_test",
                ["Database:Username"] = "postgres",
                ["Database:Password"] = "postgres",

                // Auth must actually fire in tests
                ["ApiKeyAuth:BypassInDevelopment"] = "false",

                // Cloud storage stays unconfigured — we replace the services below
                ["CloudStorage:UseManagedIdentity"] = "false",
                ["CloudStorage:ConnectionString"]   = "UseDevelopmentStorage=true",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Disable background services (HealthMonitor, QuotaFlush) — they add nondeterminism.
            services.RemoveAll(typeof(IHostedService));

            // Replace cloud storage with stubs so startup doesn't hit Azure.
            services.RemoveAll(typeof(ICloudStorageService));
            services.RemoveAll(typeof(ICloudStorageInitializationService));
            services.AddSingleton(Substitute.For<ICloudStorageService>());
            services.AddSingleton(Substitute.For<ICloudStorageInitializationService>());
        });
    }

    /// <summary>
    /// Inserts an API key directly via DbContext (bypasses ApiKeyService.CreateAsync so we
    /// can control state — IsActive, ExpiresAt, RevokedAt, MonthlyRequestCount, etc.).
    /// Returns the raw <c>pfa_sk_*</c> key the caller would put in the X-Api-Key header.
    /// </summary>
    public async Task<string> SeedKeyAsync(
        string ownerId,
        SubscriptionTier tier,
        bool isActive = true,
        DateTime? expiresAt = null,
        DateTime? revokedAt = null,
        long monthlyRequestCount = 0,
        DateTime? quotaResetAt = null)
    {
        var raw = GenerateRawKey();
        var prefix = raw[..12];
        var hash = Sha256(raw);
        var now = DateTime.UtcNow;

        using var scope = Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<PreflightApiDbContext>();

        ctx.ApiKeys.Add(new ApiKey
        {
            OwnerId = ownerId,
            Prefix = prefix,
            KeyHash = hash,
            Name = $"Test key {ownerId}",
            Tier = tier,
            IsActive = isActive,
            CreatedAt = now,
            ExpiresAt = expiresAt,
            RevokedAt = revokedAt,
            MonthlyRequestCount = monthlyRequestCount,
            QuotaResetAt = quotaResetAt
                ?? new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1)
        });

        await ctx.SaveChangesAsync();
        return raw;
    }

    public HttpClient CreateClientWithKey(string rawKey)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", rawKey);
        return client;
    }

    private static string GenerateRawKey()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        var sb = new StringBuilder(KeyPrefix, KeyPrefix.Length + 32);
        for (int i = 0; i < 32; i++) sb.Append(Base62Chars[bytes[i] % Base62Chars.Length]);
        return sb.ToString();
    }

    private static string Sha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

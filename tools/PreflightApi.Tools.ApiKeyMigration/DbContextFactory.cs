using Microsoft.EntityFrameworkCore;
using Npgsql;
using PreflightApi.Infrastructure.Data;

namespace PreflightApi.Tools.ApiKeyMigration;

internal static class DbContextFactory
{
    public static PreflightApiDbContext Create(string connectionString)
    {
        var dsBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dsBuilder.UseNetTopologySuite();
        dsBuilder.EnableDynamicJson();
        var dataSource = dsBuilder.Build();

        var options = new DbContextOptionsBuilder<PreflightApiDbContext>()
            .UseNpgsql(dataSource, npg => npg.UseNetTopologySuite())
            .Options;

        return new PreflightApiDbContext(options);
    }
}

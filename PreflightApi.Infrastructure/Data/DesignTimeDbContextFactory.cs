using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace PreflightApi.Infrastructure.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PreflightApiDbContext>
    {
        public PreflightApiDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                "Host=localhost;Database=preflightapi_development_database;Username=preflightapi_development_user;Password=localdevpassword;Port=5432";

            var optionsBuilder = new DbContextOptionsBuilder<PreflightApiDbContext>();
            optionsBuilder.UseNpgsql(connectionString,
                npgsqlOptions => npgsqlOptions.UseNetTopologySuite());

            return new PreflightApiDbContext(optionsBuilder.Options);
        }
    }
}
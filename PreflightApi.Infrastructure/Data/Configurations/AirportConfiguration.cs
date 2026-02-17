using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreflightApi.Domain.Entities;

namespace PreflightApi.Infrastructure.Data.Configurations
{
    public class AirportConfiguration : IEntityTypeConfiguration<Airport>
    {
        public void Configure(EntityTypeBuilder<Airport> builder)
        {
            builder.ToTable("airports");

            builder.HasKey(e => e.SiteNo);

            // Required string property
            builder.Property(e => e.SiteNo).IsRequired();

            // Decimal properties with precision
            builder.Property(e => e.LatDecimal).HasColumnType("decimal(10,8)");
            builder.Property(e => e.LongDecimal).HasColumnType("decimal(11,8)");
            builder.Property(e => e.Elev).HasColumnType("decimal(6,1)");
            builder.Property(e => e.MagVarn).HasColumnType("decimal(2,0)");
            builder.Property(e => e.DistCityToAirport).HasColumnType("decimal(2,0)");

            // Latitude components
            builder.Property(e => e.LatDeg).HasColumnType("int");
            builder.Property(e => e.LatMin).HasColumnType("int");
            builder.Property(e => e.LatSec).HasColumnType("decimal(6,2)");

            // Longitude components
            builder.Property(e => e.LongDeg).HasColumnType("int");
            builder.Property(e => e.LongMin).HasColumnType("int");
            builder.Property(e => e.LongSec).HasColumnType("decimal(6,2)");

            // Spatial column and index
            builder.Property(e => e.Location)
                .HasColumnType("geography(Point, 4326)");

            // Indexes
            builder.HasIndex(e => e.IcaoId);
            builder.HasIndex(e => e.ArptId);
            builder.HasIndex(e => e.StateCode);
            builder.HasIndex(a => new { a.StateCode, a.IcaoId });
            builder.HasIndex(a => new { a.StateCode, a.ArptId });

            builder.HasIndex(e => e.Location)
                .HasMethod("gist");
        }
    }
}

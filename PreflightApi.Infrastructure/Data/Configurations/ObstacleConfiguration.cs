using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreflightApi.Domain.Entities;

namespace PreflightApi.Infrastructure.Data.Configurations;

public class ObstacleConfiguration : IEntityTypeConfiguration<Obstacle>
{
    public void Configure(EntityTypeBuilder<Obstacle> builder)
    {
        builder.ToTable("obstacles");

        builder.HasKey(e => e.OasNumber);

        builder.Property(e => e.OasNumber).IsRequired();
        builder.Property(e => e.OasCode).IsRequired();
        builder.Property(e => e.ObstacleNumber).IsRequired();
        builder.Property(e => e.LatDecimal).HasColumnType("decimal(10,8)");
        builder.Property(e => e.LongDecimal).HasColumnType("decimal(11,8)");
        builder.Property(e => e.LatSeconds).HasColumnType("decimal(6,2)");
        builder.Property(e => e.LongSeconds).HasColumnType("decimal(6,2)");

        builder.Property(e => e.Location)
            .HasColumnType("geography(Point, 4326)");

        // Indexes for efficient querying
        builder.HasIndex(e => e.OasNumber).IsUnique();
        builder.HasIndex(e => e.StateId);
        builder.HasIndex(e => e.ObstacleType);
        builder.HasIndex(e => e.HeightAgl);
        builder.HasIndex(e => e.HeightAmsl);

        // Spatial index for location-based queries
        builder.HasIndex(e => e.Location)
            .HasMethod("gist");
    }
}

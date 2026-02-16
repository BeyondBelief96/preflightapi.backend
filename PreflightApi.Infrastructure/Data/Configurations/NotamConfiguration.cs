using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreflightApi.Domain.Entities;

namespace PreflightApi.Infrastructure.Data.Configurations;

public class NotamConfiguration : IEntityTypeConfiguration<Notam>
{
    public void Configure(EntityTypeBuilder<Notam> builder)
    {
        builder.ToTable("notams");

        builder.HasKey(e => e.NmsId);

        builder.Property(e => e.NmsId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.Location)
            .HasMaxLength(10);

        builder.Property(e => e.IcaoLocation)
            .HasMaxLength(10);

        builder.Property(e => e.Classification)
            .HasMaxLength(20);

        builder.Property(e => e.NotamType)
            .HasMaxLength(5);

        builder.Property(e => e.NotamNumber)
            .HasMaxLength(10);

        builder.Property(e => e.NotamYear)
            .HasMaxLength(4);

        builder.Property(e => e.AccountId)
            .HasMaxLength(10);

        builder.Property(e => e.AirportName)
            .HasMaxLength(100);

        builder.Property(e => e.FeatureJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.Geometry)
            .HasColumnType("geometry(Geometry, 4326)");

        // Indexes for common query patterns
        builder.HasIndex(e => e.Location);
        builder.HasIndex(e => e.IcaoLocation);
        builder.HasIndex(e => e.Classification);
        builder.HasIndex(e => e.EffectiveStart);
        builder.HasIndex(e => e.EffectiveEnd);
        builder.HasIndex(e => e.CancelationDate);
        builder.HasIndex(e => e.LastUpdated);
        builder.HasIndex(e => e.AccountId);
        builder.HasIndex(e => new { e.NotamNumber, e.NotamYear });

        // Spatial index for geometry queries
        builder.HasIndex(e => e.Geometry)
            .HasMethod("gist");
    }
}

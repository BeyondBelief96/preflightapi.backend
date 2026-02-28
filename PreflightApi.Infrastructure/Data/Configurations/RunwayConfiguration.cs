using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreflightApi.Domain.Entities;

namespace PreflightApi.Infrastructure.Data.Configurations;

public class RunwayConfiguration : IEntityTypeConfiguration<Runway>
{
    public void Configure(EntityTypeBuilder<Runway> builder)
    {
        builder.ToTable("runways");

        // Primary Key
        builder.HasKey(e => e.Id);

        // Required fields
        builder.Property(e => e.SiteNo)
            .IsRequired();

        builder.Property(e => e.RunwayId)
            .IsRequired();

        // Relationship to Airport (via SiteNo alternate key)
        builder.HasOne<Airport>()
            .WithMany()
            .HasForeignKey(r => r.SiteNo)
            .HasPrincipalKey(a => a.SiteNo)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-many relationship with RunwayEnds
        builder.HasMany(r => r.RunwayEnds)
            .WithOne(re => re.Runway)
            .HasForeignKey(re => re.RunwayFk)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: one runway ID per airport
        builder.HasIndex(r => new { r.SiteNo, r.RunwayId })
            .IsUnique();

        // Geometry column (ArcGIS runway polygon)
        builder.Property(e => e.Geometry)
            .HasColumnType("geometry(Polygon, 4326)");

        builder.HasIndex(e => e.Geometry)
            .HasMethod("gist");

        // Query indexes
        builder.HasIndex(r => r.SiteNo);
        builder.HasIndex(r => r.Length);
        builder.HasIndex(r => r.SurfaceTypeCode);
    }
}

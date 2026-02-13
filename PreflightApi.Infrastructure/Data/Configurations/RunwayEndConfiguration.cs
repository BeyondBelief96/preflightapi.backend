using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreflightApi.Domain.Entities;

namespace PreflightApi.Infrastructure.Data.Configurations;

public class RunwayEndConfiguration : IEntityTypeConfiguration<RunwayEnd>
{
    public void Configure(EntityTypeBuilder<RunwayEnd> builder)
    {
        builder.ToTable("runway_ends");

        // Primary Key
        builder.HasKey(e => e.Id);

        // Required fields
        builder.Property(e => e.SiteNo)
            .IsRequired();

        builder.Property(e => e.RunwayIdRef)
            .IsRequired();

        builder.Property(e => e.RunwayEndId)
            .IsRequired();

        // Decimal precision for coordinates and elevations
        builder.Property(e => e.LatDecimal)
            .HasColumnType("decimal(10,8)");

        builder.Property(e => e.LongDecimal)
            .HasColumnType("decimal(11,8)");

        builder.Property(e => e.Elevation)
            .HasColumnType("decimal(7,1)");

        builder.Property(e => e.ThresholdCrossingHeight)
            .HasColumnType("decimal(5,1)");

        builder.Property(e => e.VisualGlidePathAngle)
            .HasColumnType("decimal(4,2)");

        builder.Property(e => e.DisplacedThresholdLatDecimal)
            .HasColumnType("decimal(10,8)");

        builder.Property(e => e.DisplacedThresholdLongDecimal)
            .HasColumnType("decimal(11,8)");

        builder.Property(e => e.DisplacedThresholdElev)
            .HasColumnType("decimal(7,1)");

        builder.Property(e => e.TouchdownZoneElev)
            .HasColumnType("decimal(7,1)");

        // DMS Coordinates - Runway End
        builder.Property(e => e.RwyEndLatSec).HasColumnType("decimal(6,2)");
        builder.Property(e => e.RwyEndLongSec).HasColumnType("decimal(6,2)");

        // DMS Coordinates - Displaced Threshold
        builder.Property(e => e.DisplacedThrLatSec).HasColumnType("decimal(6,2)");
        builder.Property(e => e.DisplacedThrLongSec).HasColumnType("decimal(6,2)");

        // Codes & Gradient
        builder.Property(e => e.RunwayGradient).HasColumnType("decimal(5,2)");

        // LAHSO
        builder.Property(e => e.LahsoLatDecimal).HasColumnType("decimal(10,8)");
        builder.Property(e => e.LahsoLongDecimal).HasColumnType("decimal(11,8)");

        // Relationship to Runway is configured in RunwayConfiguration

        // Unique constraint: one runway end per runway
        builder.HasIndex(re => new { re.RunwayFk, re.RunwayEndId })
            .IsUnique()
            .HasFilter("\"runway_fk\" IS NOT NULL");

        // Index for linking during data sync
        builder.HasIndex(re => new { re.SiteNo, re.RunwayIdRef, re.RunwayEndId })
            .IsUnique();

        // Query index
        builder.HasIndex(re => re.RunwayFk);
    }
}

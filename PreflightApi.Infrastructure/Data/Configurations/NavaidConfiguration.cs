using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreflightApi.Domain.Entities;

namespace PreflightApi.Infrastructure.Data.Configurations
{
    public class NavaidConfiguration : IEntityTypeConfiguration<Navaid>
    {
        public void Configure(EntityTypeBuilder<Navaid> builder)
        {
            builder.ToTable("navaids");

            // Primary Key
            builder.HasKey(e => e.Id);

            // Required fields
            builder.Property(e => e.NavId)
                .IsRequired()
                .HasMaxLength(4);

            builder.Property(e => e.NavType)
                .IsRequired()
                .HasMaxLength(25);

            builder.Property(e => e.City)
                .IsRequired()
                .HasMaxLength(40);

            builder.Property(e => e.CountryCode)
                .IsRequired()
                .HasMaxLength(2);

            builder.Property(e => e.NavStatus)
                .IsRequired()
                .HasMaxLength(30);

            builder.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(30);

            builder.Property(e => e.CountryName)
                .IsRequired()
                .HasMaxLength(30);

            builder.Property(e => e.NasUseFlag)
                .IsRequired()
                .HasMaxLength(1);

            builder.Property(e => e.PublicUseFlag)
                .IsRequired()
                .HasMaxLength(1);

            // Optional fields with max lengths
            builder.Property(e => e.StateCode)
                .HasMaxLength(2);

            builder.Property(e => e.StateName)
                .HasMaxLength(30);

            builder.Property(e => e.RegionCode)
                .HasMaxLength(3);

            builder.Property(e => e.FanMarker)
                .HasMaxLength(30);

            builder.Property(e => e.Owner)
                .HasMaxLength(50);

            builder.Property(e => e.Operator)
                .HasMaxLength(50);

            builder.Property(e => e.NdbClassCode)
                .HasMaxLength(11);

            builder.Property(e => e.OperHours)
                .HasMaxLength(11);

            builder.Property(e => e.HighAltArtccId)
                .HasMaxLength(4);

            builder.Property(e => e.HighArtccName)
                .HasMaxLength(30);

            builder.Property(e => e.LowAltArtccId)
                .HasMaxLength(4);

            builder.Property(e => e.LowArtccName)
                .HasMaxLength(30);

            builder.Property(e => e.LatHemis)
                .HasMaxLength(1);

            builder.Property(e => e.LongHemis)
                .HasMaxLength(1);

            builder.Property(e => e.SurveyAccuracyCode)
                .HasMaxLength(1);

            builder.Property(e => e.TacanDmeStatus)
                .HasMaxLength(30);

            builder.Property(e => e.TacanDmeLatHemis)
                .HasMaxLength(1);

            builder.Property(e => e.TacanDmeLongHemis)
                .HasMaxLength(1);

            builder.Property(e => e.MagVarnHemis)
                .HasMaxLength(1);

            builder.Property(e => e.SimulVoiceFlag)
                .HasMaxLength(1);

            builder.Property(e => e.AutoVoiceIdFlag)
                .HasMaxLength(1);

            builder.Property(e => e.MntCatCode)
                .HasMaxLength(1);

            builder.Property(e => e.VoiceCall)
                .HasMaxLength(60);

            builder.Property(e => e.Chan)
                .HasMaxLength(4);

            builder.Property(e => e.MkrIdent)
                .HasMaxLength(30);

            builder.Property(e => e.MkrShape)
                .HasMaxLength(1);

            builder.Property(e => e.AltCode)
                .HasMaxLength(2);

            builder.Property(e => e.DmeSsv)
                .HasMaxLength(2);

            builder.Property(e => e.LowNavOnHighChartFlag)
                .HasMaxLength(1);

            builder.Property(e => e.ZMkrFlag)
                .HasMaxLength(1);

            builder.Property(e => e.FssId)
                .HasMaxLength(4);

            builder.Property(e => e.FssName)
                .HasMaxLength(30);

            builder.Property(e => e.FssHours)
                .HasMaxLength(65);

            builder.Property(e => e.NotamId)
                .HasMaxLength(4);

            builder.Property(e => e.QuadIdent)
                .HasMaxLength(20);

            builder.Property(e => e.PitchFlag)
                .HasMaxLength(1);

            builder.Property(e => e.CatchFlag)
                .HasMaxLength(1);

            builder.Property(e => e.SuaAtcaaFlag)
                .HasMaxLength(1);

            builder.Property(e => e.RestrictionFlag)
                .HasMaxLength(1);

            builder.Property(e => e.HiwasFlag)
                .HasMaxLength(1);

            // Decimal precision for coordinates
            builder.Property(e => e.LatSec)
                .HasColumnType("decimal(6,4)");

            builder.Property(e => e.LatDecimal)
                .HasColumnType("decimal(10,8)");

            builder.Property(e => e.LongSec)
                .HasColumnType("decimal(6,4)");

            builder.Property(e => e.LongDecimal)
                .HasColumnType("decimal(11,8)");

            builder.Property(e => e.TacanDmeLatSec)
                .HasColumnType("decimal(6,4)");

            builder.Property(e => e.TacanDmeLatDecimal)
                .HasColumnType("decimal(10,8)");

            builder.Property(e => e.TacanDmeLongSec)
                .HasColumnType("decimal(6,4)");

            builder.Property(e => e.TacanDmeLongDecimal)
                .HasColumnType("decimal(11,8)");

            builder.Property(e => e.Elev)
                .HasColumnType("decimal(6,1)");

            builder.Property(e => e.Freq)
                .HasColumnType("decimal(5,2)");

            // Column type specifications
            builder.Property(e => e.EffectiveDate)
                .HasColumnType("date");

            // JSONB columns
            builder.Property(e => e.CheckpointsJson)
                .HasColumnType("jsonb");

            builder.Property(e => e.RemarksJson)
                .HasColumnType("jsonb");

            // Composite unique index
            builder.HasIndex(e => new { e.NavId, e.NavType, e.CountryCode, e.City })
                .IsUnique()
                .HasFilter("\"nav_id\" IS NOT NULL AND \"nav_type\" IS NOT NULL AND \"country_code\" IS NOT NULL AND \"city\" IS NOT NULL");

            // Spatial indexes (GIST)
            builder.HasIndex(e => e.Location)
                .HasMethod("gist");

            builder.HasIndex(e => e.TacanDmeLocation)
                .HasMethod("gist");

            // Regular indexes for lookups
            builder.HasIndex(e => e.NavId);
            builder.HasIndex(e => e.NavType);
            builder.HasIndex(e => e.StateCode);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.ValueObjects.NavigationalAids;

namespace PreflightApi.Infrastructure.Data.Configurations;

public class NavigationalAidConfiguration : IEntityTypeConfiguration<NavigationalAid>
{
    public void Configure(EntityTypeBuilder<NavigationalAid> builder)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        builder.ToTable("navigational_aids");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.NavId).IsRequired().HasMaxLength(30);
        builder.Property(e => e.NavType).IsRequired().HasMaxLength(20);
        builder.Property(e => e.StateCode).HasMaxLength(2);
        builder.Property(e => e.City).HasMaxLength(40);
        builder.Property(e => e.CountryCode).HasMaxLength(2);
        builder.Property(e => e.NavStatus).HasMaxLength(30);
        builder.Property(e => e.Name).HasMaxLength(50);
        builder.Property(e => e.StateName).HasMaxLength(30);
        builder.Property(e => e.RegionCode).HasMaxLength(3);
        builder.Property(e => e.CountryName).HasMaxLength(30);
        builder.Property(e => e.FanMarker).HasMaxLength(10);
        builder.Property(e => e.Owner).HasMaxLength(50);
        builder.Property(e => e.Operator).HasMaxLength(50);
        builder.Property(e => e.NasUseFlag).HasMaxLength(1);
        builder.Property(e => e.PublicUseFlag).HasMaxLength(1);
        builder.Property(e => e.NdbClassCode).HasMaxLength(10);
        builder.Property(e => e.OperHours).HasMaxLength(30);
        builder.Property(e => e.HighAltArtccId).HasMaxLength(4);
        builder.Property(e => e.HighArtccName).HasMaxLength(30);
        builder.Property(e => e.LowAltArtccId).HasMaxLength(4);
        builder.Property(e => e.LowArtccName).HasMaxLength(30);
        builder.Property(e => e.LatDecimal).HasColumnType("decimal(10,8)");
        builder.Property(e => e.LongDecimal).HasColumnType("decimal(11,8)");
        builder.Property(e => e.SurveyAccuracyCode).HasMaxLength(1);
        builder.Property(e => e.TacanDmeStatus).HasMaxLength(30);
        builder.Property(e => e.TacanDmeLatDecimal).HasColumnType("decimal(10,8)");
        builder.Property(e => e.TacanDmeLongDecimal).HasColumnType("decimal(11,8)");
        builder.Property(e => e.Elevation).HasColumnType("decimal(7,1)");
        builder.Property(e => e.MagVarn).HasMaxLength(10);
        builder.Property(e => e.MagVarnHemis).HasMaxLength(1);
        builder.Property(e => e.MagVarnYear).HasMaxLength(4);
        builder.Property(e => e.SimulVoiceFlag).HasMaxLength(1);
        builder.Property(e => e.PowerOutput).HasMaxLength(10);
        builder.Property(e => e.AutoVoiceIdFlag).HasMaxLength(1);
        builder.Property(e => e.MonitoringCategoryCode).HasMaxLength(3);
        builder.Property(e => e.VoiceCall).HasMaxLength(30);
        builder.Property(e => e.Channel).HasMaxLength(10);
        builder.Property(e => e.Frequency).HasMaxLength(20);
        builder.Property(e => e.MarkerIdent).HasMaxLength(10);
        builder.Property(e => e.MarkerShape).HasMaxLength(10);
        builder.Property(e => e.MarkerBearing).HasMaxLength(10);
        builder.Property(e => e.AltitudeCode).HasMaxLength(3);
        builder.Property(e => e.DmeSsv).HasMaxLength(10);
        builder.Property(e => e.LowNavOnHighChartFlag).HasMaxLength(1);
        builder.Property(e => e.ZMarkerFlag).HasMaxLength(1);
        builder.Property(e => e.FssId).HasMaxLength(4);
        builder.Property(e => e.FssName).HasMaxLength(30);
        builder.Property(e => e.FssHours).HasMaxLength(100);
        builder.Property(e => e.NotamId).HasMaxLength(4);
        builder.Property(e => e.QuadIdent).HasMaxLength(10);
        builder.Property(e => e.PitchFlag).HasMaxLength(3);
        builder.Property(e => e.CatchFlag).HasMaxLength(3);
        builder.Property(e => e.SuaAtcaaFlag).HasMaxLength(3);
        builder.Property(e => e.RestrictionFlag).HasMaxLength(3);
        builder.Property(e => e.HiwasFlag).HasMaxLength(3);

        builder.Property(e => e.EffectiveDate).HasColumnType("date");

        builder.HasIndex(e => e.NavId);
        builder.HasIndex(e => e.NavType);
        builder.HasIndex(e => e.StateCode);
        builder.HasIndex(e => new { e.LatDecimal, e.LongDecimal });
        builder.HasIndex(e => new { e.NavId, e.NavType })
            .IsUnique()
            .HasFilter("\"nav_id\" IS NOT NULL AND \"nav_type\" IS NOT NULL");

        builder.Property(x => x.Checkpoints)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<NavaidCheckpoint>>(v ?? "[]", jsonOptions) ?? new List<NavaidCheckpoint>(),
                new ValueComparer<List<NavaidCheckpoint>>(
                    (l, r) => JsonSerializer.Serialize(l, jsonOptions) == JsonSerializer.Serialize(r, jsonOptions),
                    v => v == null ? 0 : JsonSerializer.Serialize(v, jsonOptions).GetHashCode(),
                    v => JsonSerializer.Deserialize<List<NavaidCheckpoint>>(JsonSerializer.Serialize(v, jsonOptions), jsonOptions) ?? new List<NavaidCheckpoint>()
                ));
    }
}

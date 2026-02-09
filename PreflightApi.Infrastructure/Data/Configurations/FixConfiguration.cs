using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.ValueObjects.Fixes;

namespace PreflightApi.Infrastructure.Data.Configurations;

public class FixConfiguration : IEntityTypeConfiguration<Fix>
{
    public void Configure(EntityTypeBuilder<Fix> builder)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        builder.ToTable("fixes");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FixId).IsRequired().HasMaxLength(30);
        builder.Property(e => e.IcaoRegionCode).IsRequired().HasMaxLength(4);
        builder.Property(e => e.StateCode).HasMaxLength(2);
        builder.Property(e => e.CountryCode).HasMaxLength(2);
        builder.Property(e => e.LatDecimal).HasColumnType("decimal(10,8)");
        builder.Property(e => e.LongDecimal).HasColumnType("decimal(11,8)");
        builder.Property(e => e.FixIdOld).HasMaxLength(30);
        builder.Property(e => e.ChartingRemark).HasMaxLength(200);
        builder.Property(e => e.FixUseCode).HasMaxLength(20);
        builder.Property(e => e.ArtccIdHigh).HasMaxLength(4);
        builder.Property(e => e.ArtccIdLow).HasMaxLength(4);
        builder.Property(e => e.PitchFlag).HasMaxLength(3);
        builder.Property(e => e.CatchFlag).HasMaxLength(3);
        builder.Property(e => e.SuaAtcaaFlag).HasMaxLength(3);
        builder.Property(e => e.MinReceptionAlt).HasMaxLength(10);
        builder.Property(e => e.Compulsory).HasMaxLength(10);

        builder.Property(e => e.EffectiveDate).HasColumnType("date");

        builder.HasIndex(e => e.FixId);
        builder.HasIndex(e => e.StateCode);
        builder.HasIndex(e => e.FixUseCode);
        builder.HasIndex(e => new { e.LatDecimal, e.LongDecimal });
        builder.HasIndex(e => new { e.FixId, e.IcaoRegionCode, e.StateCode, e.CountryCode })
            .IsUnique()
            .HasFilter("\"fix_id\" IS NOT NULL AND \"icao_region_code\" IS NOT NULL");

        builder.Property(x => x.ChartingTypes)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<string>>(v ?? "[]", jsonOptions) ?? new List<string>(),
                new ValueComparer<List<string>>(
                    (l, r) => JsonSerializer.Serialize(l, jsonOptions) == JsonSerializer.Serialize(r, jsonOptions),
                    v => v == null ? 0 : JsonSerializer.Serialize(v, jsonOptions).GetHashCode(),
                    v => JsonSerializer.Deserialize<List<string>>(JsonSerializer.Serialize(v, jsonOptions), jsonOptions) ?? new List<string>()
                ));

        builder.Property(x => x.NavaidReferences)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<FixNavaidReference>>(v ?? "[]", jsonOptions) ?? new List<FixNavaidReference>(),
                new ValueComparer<List<FixNavaidReference>>(
                    (l, r) => JsonSerializer.Serialize(l, jsonOptions) == JsonSerializer.Serialize(r, jsonOptions),
                    v => v == null ? 0 : JsonSerializer.Serialize(v, jsonOptions).GetHashCode(),
                    v => JsonSerializer.Deserialize<List<FixNavaidReference>>(JsonSerializer.Serialize(v, jsonOptions), jsonOptions) ?? new List<FixNavaidReference>()
                ));
    }
}

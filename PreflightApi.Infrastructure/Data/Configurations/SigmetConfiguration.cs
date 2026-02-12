using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.ValueObjects.Sigmets;

namespace PreflightApi.Infrastructure.Data.Configurations
{
    public class SigmetConfiguration : IEntityTypeConfiguration<Sigmet>
    {
        public void Configure(EntityTypeBuilder<Sigmet> builder)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            builder.HasIndex(e => e.ValidTimeFrom);
            builder.HasIndex(e => e.ValidTimeTo);
            builder.HasIndex(e => e.SigmetType);

            builder.Property(x => x.Altitude)
                .HasConversion(
                    v => v == null ? "{}" : JsonSerializer.Serialize(v, jsonOptions),
                    v => string.IsNullOrEmpty(v) ? new SigmetAltitude() : JsonSerializer.Deserialize<SigmetAltitude>(v, jsonOptions) ?? new SigmetAltitude(),
                    new ValueComparer<SigmetAltitude>(
                        (l, r) => (l == null && r == null) || (l != null && r != null && JsonSerializer.Serialize(l, jsonOptions) == JsonSerializer.Serialize(r, jsonOptions)),
                        v => v == null ? 0 : JsonSerializer.Serialize(v, jsonOptions).GetHashCode(),
                        v => v == null ? new SigmetAltitude() : JsonSerializer.Deserialize<SigmetAltitude>(JsonSerializer.Serialize(v, jsonOptions), jsonOptions) ?? new SigmetAltitude()
                    ));

            builder.Property(x => x.Hazard)
                .HasConversion(
                    v => v == null ? "{}" : JsonSerializer.Serialize(v, jsonOptions),
                    v => string.IsNullOrEmpty(v) ? new SigmetHazard() : JsonSerializer.Deserialize<SigmetHazard>(v, jsonOptions) ?? new SigmetHazard(),
                    new ValueComparer<SigmetHazard>(
                        (l, r) => (l == null && r == null) || (l != null && r != null && JsonSerializer.Serialize(l, jsonOptions) == JsonSerializer.Serialize(r, jsonOptions)),
                        v => v == null ? 0 : JsonSerializer.Serialize(v, jsonOptions).GetHashCode(),
                        v => v == null ? new SigmetHazard() : JsonSerializer.Deserialize<SigmetHazard>(JsonSerializer.Serialize(v, jsonOptions), jsonOptions) ?? new SigmetHazard()
                    ));

            builder.Property(x => x.Areas)
                .HasConversion(
                    v => v == null ? "[]" : JsonSerializer.Serialize(v, jsonOptions),
                    v => string.IsNullOrEmpty(v) ? new List<SigmetArea>() : JsonSerializer.Deserialize<List<SigmetArea>>(v, jsonOptions) ?? new List<SigmetArea>(),
                    new ValueComparer<List<SigmetArea>>(
                        (l, r) => (l == null && r == null) || (l != null && r != null && JsonSerializer.Serialize(l, jsonOptions) == JsonSerializer.Serialize(r, jsonOptions)),
                        v => v == null ? 0 : JsonSerializer.Serialize(v, jsonOptions).GetHashCode(),
                        v => v == null ? new List<SigmetArea>() : JsonSerializer.Deserialize<List<SigmetArea>>(JsonSerializer.Serialize(v, jsonOptions), jsonOptions) ?? new List<SigmetArea>()
                    ));
        }
    }
}

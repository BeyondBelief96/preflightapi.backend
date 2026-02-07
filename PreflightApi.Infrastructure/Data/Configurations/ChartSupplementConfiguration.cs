using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using PreflightApi.Domain.Entities;

namespace PreflightApi.Infrastructure.Data.Configurations
{
    public class ChartSupplementConfiguration : IEntityTypeConfiguration<ChartSupplement>
    {
        public void Configure(EntityTypeBuilder<ChartSupplement> builder)
        {
            builder.HasIndex(e => e.AirportCode);
            builder.HasIndex(e => e.NavigationalAidName);
            builder.HasIndex(e => new { e.FileName, e.AirportCode })
                .IsUnique()
                .HasFilter("\"airport_code\" IS NOT NULL");
            builder.HasIndex(e => new { e.FileName, e.NavigationalAidName })
                .IsUnique()
                .HasFilter("\"navigational_aid_name\" IS NOT NULL AND \"airport_code\" IS NULL");

            builder.Property(e => e.AirportName)
                .HasMaxLength(255);

            builder.Property(e => e.AirportCity)
                .HasMaxLength(255);

            builder.Property(e => e.AirportCode)
                .HasMaxLength(10);

            builder.Property(e => e.NavigationalAidName)
                .HasMaxLength(255);

            builder.Property(e => e.FileName)
                .HasMaxLength(255);
        }
    }
}

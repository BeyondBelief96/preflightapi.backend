using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreflightApi.Domain.Entities;

namespace PreflightApi.Infrastructure.Data.Configurations;

public class WeatherStationConfiguration : IEntityTypeConfiguration<WeatherStation>
{
    public void Configure(EntityTypeBuilder<WeatherStation> builder)
    {
        builder.ToTable("weather_stations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.AsosAwosId).IsRequired().HasMaxLength(30);
        builder.Property(e => e.AsosAwosType).IsRequired().HasMaxLength(20);
        builder.Property(e => e.StateCode).HasMaxLength(2);
        builder.Property(e => e.City).HasMaxLength(40);
        builder.Property(e => e.CountryCode).HasMaxLength(2);
        builder.Property(e => e.NavaidFlag).HasMaxLength(1);
        builder.Property(e => e.LatDecimal).HasColumnType("decimal(10,8)");
        builder.Property(e => e.LongDecimal).HasColumnType("decimal(11,8)");
        builder.Property(e => e.Elevation).HasColumnType("decimal(7,1)");
        builder.Property(e => e.SurveyMethodCode).HasMaxLength(1);
        builder.Property(e => e.PhoneNo).HasMaxLength(20);
        builder.Property(e => e.SecondPhoneNo).HasMaxLength(20);
        builder.Property(e => e.SiteNo).HasMaxLength(30);
        builder.Property(e => e.SiteTypeCode).HasMaxLength(10);

        builder.Property(e => e.EffectiveDate).HasColumnType("date");
        builder.Property(e => e.CommissionedDate).HasColumnType("date");

        builder.HasIndex(e => e.AsosAwosId);
        builder.HasIndex(e => e.AsosAwosType);
        builder.HasIndex(e => e.StateCode);
        builder.HasIndex(e => e.SiteNo);
        builder.HasIndex(e => new { e.LatDecimal, e.LongDecimal });
        builder.HasIndex(e => new { e.AsosAwosId, e.AsosAwosType })
            .IsUnique()
            .HasFilter("\"asos_awos_id\" IS NOT NULL AND \"asos_awos_type\" IS NOT NULL");
    }
}

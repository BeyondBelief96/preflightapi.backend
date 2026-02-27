using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreflightApi.Domain.Entities;

namespace PreflightApi.Infrastructure.Data.Configurations
{
    public class ServiceHealthAlertStateConfiguration : IEntityTypeConfiguration<ServiceHealthAlertState>
    {
        public void Configure(EntityTypeBuilder<ServiceHealthAlertState> builder)
        {
            builder.Property(e => e.ServiceName).HasMaxLength(100);
            builder.Property(e => e.LastKnownStatus).HasMaxLength(20);
            builder.Property(e => e.LastAlertSeverity).HasMaxLength(20);
        }
    }
}

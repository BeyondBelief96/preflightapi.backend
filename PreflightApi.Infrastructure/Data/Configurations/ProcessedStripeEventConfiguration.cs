using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreflightApi.Domain.Entities;

namespace PreflightApi.Infrastructure.Data.Configurations
{
    public class ProcessedStripeEventConfiguration : IEntityTypeConfiguration<ProcessedStripeEvent>
    {
        public void Configure(EntityTypeBuilder<ProcessedStripeEvent> builder)
        {
            builder.HasKey(e => e.EventId);
            builder.Property(e => e.ProcessedAt).HasDefaultValueSql("now()");
            builder.HasIndex(e => e.ProcessedAt);
        }
    }
}

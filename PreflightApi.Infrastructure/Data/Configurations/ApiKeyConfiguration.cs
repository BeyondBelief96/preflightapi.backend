using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreflightApi.Domain.Entities;

namespace PreflightApi.Infrastructure.Data.Configurations
{
    public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
    {
        public void Configure(EntityTypeBuilder<ApiKey> builder)
        {
            builder.HasIndex(e => e.KeyHash).IsUnique();
            builder.HasIndex(e => e.Prefix).IsUnique();
            builder.HasIndex(e => e.OwnerId);
            builder.HasIndex(e => e.StripeCustomerId)
                .HasFilter("\"stripe_customer_id\" IS NOT NULL");
            builder.HasIndex(e => e.StripeSubscriptionId)
                .HasFilter("\"stripe_subscription_id\" IS NOT NULL");

            builder.Property(e => e.Tier)
                .HasConversion<string>()
                .HasMaxLength(30);

            builder.Property(e => e.IsActive).HasDefaultValue(true);
            builder.Property(e => e.MonthlyRequestCount).HasDefaultValue(0L);
            builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        }
    }
}

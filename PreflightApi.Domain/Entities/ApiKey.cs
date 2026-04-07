using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PreflightApi.Domain.Enums;

namespace PreflightApi.Domain.Entities
{
    [Table("api_keys")]
    public class ApiKey
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        /// <summary>Clerk user ID that owns this key.</summary>
        [Column("owner_id")]
        [Required]
        [MaxLength(64)]
        public string OwnerId { get; set; } = string.Empty;

        /// <summary>Stripe customer ID, if the user has one.</summary>
        [Column("stripe_customer_id")]
        [MaxLength(128)]
        public string? StripeCustomerId { get; set; }

        /// <summary>Stripe subscription ID that determines the tier.</summary>
        [Column("stripe_subscription_id")]
        [MaxLength(128)]
        public string? StripeSubscriptionId { get; set; }

        /// <summary>First 12 characters of the key for display/identification (e.g., "pfa_sk_a1b2c").</summary>
        [Column("prefix")]
        [Required]
        [MaxLength(12)]
        public string Prefix { get; set; } = string.Empty;

        /// <summary>SHA-256 hash of the full API key. The raw key is never stored.</summary>
        [Column("key_hash")]
        [Required]
        [MaxLength(128)]
        public string KeyHash { get; set; } = string.Empty;

        /// <summary>User-chosen label for identifying this key.</summary>
        [Column("name")]
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Subscription tier that determines rate limits, quota, and endpoint access.</summary>
        [Column("tier")]
        public SubscriptionTier Tier { get; set; } = SubscriptionTier.StudentPilot;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("last_used_at")]
        public DateTime? LastUsedAt { get; set; }

        [Column("revoked_at")]
        public DateTime? RevokedAt { get; set; }

        [Column("expires_at")]
        public DateTime? ExpiresAt { get; set; }

        /// <summary>Number of requests made in the current billing period.</summary>
        [Column("monthly_request_count")]
        public long MonthlyRequestCount { get; set; }

        /// <summary>UTC timestamp when the monthly quota resets.</summary>
        [Column("quota_reset_at")]
        public DateTime QuotaResetAt { get; set; }
    }
}

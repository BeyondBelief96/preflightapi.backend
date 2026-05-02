using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PreflightApi.Domain.Entities
{
    [Table("processed_stripe_events")]
    public class ProcessedStripeEvent
    {
        [Key]
        [Column("event_id")]
        [MaxLength(128)]
        public string EventId { get; set; } = string.Empty;

        [Column("event_type")]
        [MaxLength(64)]
        [Required]
        public string EventType { get; set; } = string.Empty;

        [Column("processed_at")]
        public DateTime ProcessedAt { get; set; }
    }
}

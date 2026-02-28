using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PreflightApi.Domain.Entities
{
    [Table("service_health_alert_state")]
    public class ServiceHealthAlertState
    {
        [Key]
        [Column("service_name")]
        public string ServiceName { get; set; } = string.Empty;

        [Column("last_known_status")]
        public string LastKnownStatus { get; set; } = "Healthy";

        [Column("last_alert_sent_utc")]
        public DateTime? LastAlertSentUtc { get; set; }

        [Column("last_alert_severity")]
        public string? LastAlertSeverity { get; set; }

        [Column("consecutive_failure_count")]
        public int ConsecutiveFailureCount { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PreflightApi.Domain.Entities
{
    [Table("data_sync_status")]
    public class DataSyncStatus
    {
        [Key]
        [Column("sync_type")]
        public string SyncType { get; set; } = string.Empty;

        [Column("staleness_mode")]
        public string StalenessMode { get; set; } = "TimeBased";

        [Column("staleness_threshold_minutes")]
        public int? StalenessThresholdMinutes { get; set; }

        [Column("publication_type")]
        public string? PublicationType { get; set; }

        [Column("last_successful_sync_utc")]
        public DateTime? LastSuccessfulSyncUtc { get; set; }

        [Column("last_attempted_sync_utc")]
        public DateTime? LastAttemptedSyncUtc { get; set; }

        [Column("last_sync_succeeded")]
        public bool LastSyncSucceeded { get; set; } = true;

        [Column("consecutive_failures")]
        public int ConsecutiveFailures { get; set; }

        [Column("last_error_message")]
        public string? LastErrorMessage { get; set; }

        [Column("last_successful_record_count")]
        public int LastSuccessfulRecordCount { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}

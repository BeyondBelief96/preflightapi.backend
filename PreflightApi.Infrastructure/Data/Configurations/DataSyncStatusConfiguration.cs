using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreflightApi.Domain.Constants;
using PreflightApi.Domain.Entities;

namespace PreflightApi.Infrastructure.Data.Configurations
{
    public class DataSyncStatusConfiguration : IEntityTypeConfiguration<DataSyncStatus>
    {
        public void Configure(EntityTypeBuilder<DataSyncStatus> builder)
        {
            builder.Property(e => e.SyncType).HasMaxLength(50);
            builder.Property(e => e.StalenessMode).HasMaxLength(20);
            builder.Property(e => e.PublicationType).HasMaxLength(50);
            builder.Property(e => e.LastErrorMessage).HasMaxLength(2000);
            builder.Property(e => e.LastAlertSeverity).HasMaxLength(20);

            builder.HasData(
                // Time-based sync types
                new DataSyncStatus { SyncType = SyncTypes.Metar, StalenessMode = "TimeBased", StalenessThresholdMinutes = 50 },
                new DataSyncStatus { SyncType = SyncTypes.Taf, StalenessMode = "TimeBased", StalenessThresholdMinutes = 120 },
                new DataSyncStatus { SyncType = SyncTypes.Pirep, StalenessMode = "TimeBased", StalenessThresholdMinutes = 30 },
                new DataSyncStatus { SyncType = SyncTypes.Sigmet, StalenessMode = "TimeBased", StalenessThresholdMinutes = 120 },
                new DataSyncStatus { SyncType = SyncTypes.GAirmet, StalenessMode = "TimeBased", StalenessThresholdMinutes = 120 },
                new DataSyncStatus { SyncType = SyncTypes.NotamDelta, StalenessMode = "TimeBased", StalenessThresholdMinutes = 15 },
                new DataSyncStatus { SyncType = SyncTypes.ObstacleDailyChange, StalenessMode = "TimeBased", StalenessThresholdMinutes = 2880 },
                // Cycle-based sync types
                new DataSyncStatus { SyncType = SyncTypes.Airport, StalenessMode = "CycleBased", PublicationType = "NasrSubscription_Airport" },
                new DataSyncStatus { SyncType = SyncTypes.Frequency, StalenessMode = "CycleBased", PublicationType = "NasrSubscription_Frequencies" },
                new DataSyncStatus { SyncType = SyncTypes.Airspace, StalenessMode = "CycleBased", PublicationType = "Airspaces" },
                new DataSyncStatus { SyncType = SyncTypes.SpecialUseAirspace, StalenessMode = "CycleBased", PublicationType = "SpecialUseAirspaces" },
                new DataSyncStatus { SyncType = SyncTypes.Obstacle, StalenessMode = "CycleBased", PublicationType = "Obstacles" },
                new DataSyncStatus { SyncType = SyncTypes.ChartSupplement, StalenessMode = "CycleBased", PublicationType = "ChartSupplement" },
                new DataSyncStatus { SyncType = SyncTypes.TerminalProcedure, StalenessMode = "CycleBased", PublicationType = "TerminalProcedure" },
                new DataSyncStatus { SyncType = SyncTypes.Navaid, StalenessMode = "CycleBased", PublicationType = "NasrSubscription_Navaids" },
                new DataSyncStatus { SyncType = SyncTypes.RunwayGeometry, StalenessMode = "CycleBased", PublicationType = "RunwayGeometry" }
            );
        }
    }
}

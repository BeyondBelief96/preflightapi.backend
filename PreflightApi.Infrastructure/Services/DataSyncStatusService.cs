using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.Services
{
    public class DataSyncStatusService : IDataSyncStatusService
    {
        private readonly PreflightApiDbContext _dbContext;
        private readonly ILogger<DataSyncStatusService> _logger;

        public DataSyncStatusService(PreflightApiDbContext dbContext, ILogger<DataSyncStatusService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task RecordSuccessAsync(string syncType, int recordCount = 0, CancellationToken ct = default)
        {
            try
            {
                var status = await _dbContext.DataSyncStatuses.FindAsync(new object[] { syncType }, ct);
                if (status == null)
                {
                    _logger.LogWarning("DataSyncStatus row not found for sync type '{SyncType}'. Skipping success recording.", syncType);
                    return;
                }

                var now = DateTime.UtcNow;
                status.LastSuccessfulSyncUtc = now;
                status.LastAttemptedSyncUtc = now;
                status.LastSyncSucceeded = true;
                status.ConsecutiveFailures = 0;
                status.LastSuccessfulRecordCount = recordCount;
                status.LastErrorMessage = null;
                status.UpdatedAt = now;

                await _dbContext.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to record sync success for '{SyncType}'", syncType);
            }
        }

        public async Task RecordFailureAsync(string syncType, string errorMessage, CancellationToken ct = default)
        {
            try
            {
                var status = await _dbContext.DataSyncStatuses.FindAsync(new object[] { syncType }, ct);
                if (status == null)
                {
                    _logger.LogWarning("DataSyncStatus row not found for sync type '{SyncType}'. Skipping failure recording.", syncType);
                    return;
                }

                var now = DateTime.UtcNow;
                status.LastAttemptedSyncUtc = now;
                status.LastSyncSucceeded = false;
                status.ConsecutiveFailures++;
                status.LastErrorMessage = errorMessage.Length > 2000 ? errorMessage[..2000] : errorMessage;
                status.UpdatedAt = now;

                await _dbContext.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to record sync failure for '{SyncType}'", syncType);
            }
        }

        public async Task<IReadOnlyList<DataFreshnessResult>> GetAllFreshnessAsync(CancellationToken ct = default)
        {
            var statuses = await _dbContext.DataSyncStatuses.AsNoTracking().ToListAsync(ct);
            var now = DateTime.UtcNow;

            // Load publication cycles for cycle-based evaluation
            var publicationCycles = await _dbContext.FaaPublicationCycles.AsNoTracking().ToListAsync(ct);
            var cyclesByType = publicationCycles.ToDictionary(
                c => c.PublicationType.ToString(),
                c => c);

            var results = new List<DataFreshnessResult>(statuses.Count);

            foreach (var status in statuses)
            {
                if (status.StalenessMode == "TimeBased")
                {
                    results.Add(EvaluateTimeBased(status, now));
                }
                else if (status.StalenessMode == "CycleBased")
                {
                    cyclesByType.TryGetValue(status.PublicationType ?? "", out var cycle);
                    results.Add(EvaluateCycleBased(status, now, cycle));
                }
            }

            return results;
        }

        private static DataFreshnessResult EvaluateTimeBased(DataSyncStatus status, DateTime now)
        {
            var threshold = status.StalenessThresholdMinutes ?? 60;

            if (status.LastSuccessfulSyncUtc == null)
            {
                return new DataFreshnessResult
                {
                    SyncType = status.SyncType,
                    IsFresh = false,
                    Severity = "critical",
                    StalenessMode = "TimeBased",
                    LastSuccessfulSync = null,
                    ConsecutiveFailures = status.ConsecutiveFailures,
                    LastErrorMessage = status.LastErrorMessage,
                    AgeMinutes = null,
                    ThresholdMinutes = threshold,
                    Message = $"{status.SyncType} has never been synced."
                };
            }

            var ageMinutes = (now - status.LastSuccessfulSyncUtc.Value).TotalMinutes;
            var ratio = ageMinutes / threshold;

            string severity;
            bool isFresh;
            string message;

            if (ratio < 1.0)
            {
                severity = "none";
                isFresh = true;
                message = $"{status.SyncType} is fresh ({ageMinutes:F0}m old, threshold {threshold}m).";
            }
            else if (ratio < 1.5)
            {
                severity = "info";
                isFresh = false;
                message = $"{status.SyncType} is approaching staleness ({ageMinutes:F0}m old, threshold {threshold}m).";
            }
            else if (ratio < 2.0)
            {
                severity = "warning";
                isFresh = false;
                message = $"{status.SyncType} is stale ({ageMinutes:F0}m old, threshold {threshold}m).";
            }
            else
            {
                severity = "critical";
                isFresh = false;
                message = $"{status.SyncType} is critically stale ({ageMinutes:F0}m old, threshold {threshold}m).";
            }

            return new DataFreshnessResult
            {
                SyncType = status.SyncType,
                IsFresh = isFresh,
                Severity = severity,
                StalenessMode = "TimeBased",
                LastSuccessfulSync = status.LastSuccessfulSyncUtc,
                ConsecutiveFailures = status.ConsecutiveFailures,
                LastErrorMessage = status.LastErrorMessage,
                AgeMinutes = Math.Round(ageMinutes, 1),
                ThresholdMinutes = threshold,
                Message = message
            };
        }

        private static DataFreshnessResult EvaluateCycleBased(DataSyncStatus status, DateTime now, FaaPublicationCycle? cycle)
        {
            if (cycle == null)
            {
                return new DataFreshnessResult
                {
                    SyncType = status.SyncType,
                    IsFresh = false,
                    Severity = "warning",
                    StalenessMode = "CycleBased",
                    LastSuccessfulSync = status.LastSuccessfulSyncUtc,
                    ConsecutiveFailures = status.ConsecutiveFailures,
                    LastErrorMessage = status.LastErrorMessage,
                    Message = $"{status.SyncType}: no publication cycle found for '{status.PublicationType}'."
                };
            }

            var currentCycleDate = FaaPublicationDateUtils.CalculateCurrentPublicationDate(
                cycle.KnownValidDate, cycle.CycleLengthDays);

            if (status.LastSuccessfulSyncUtc == null)
            {
                return new DataFreshnessResult
                {
                    SyncType = status.SyncType,
                    IsFresh = false,
                    Severity = "critical",
                    StalenessMode = "CycleBased",
                    LastSuccessfulSync = null,
                    ConsecutiveFailures = status.ConsecutiveFailures,
                    LastErrorMessage = status.LastErrorMessage,
                    CurrentCycleDate = currentCycleDate,
                    Message = $"{status.SyncType} has never been synced (current cycle: {currentCycleDate:yyyy-MM-dd})."
                };
            }

            // Fresh if synced after current cycle started
            if (status.LastSuccessfulSyncUtc >= currentCycleDate)
            {
                return new DataFreshnessResult
                {
                    SyncType = status.SyncType,
                    IsFresh = true,
                    Severity = "none",
                    StalenessMode = "CycleBased",
                    LastSuccessfulSync = status.LastSuccessfulSyncUtc,
                    ConsecutiveFailures = status.ConsecutiveFailures,
                    LastErrorMessage = status.LastErrorMessage,
                    CurrentCycleDate = currentCycleDate,
                    DaysPastCycleWithoutUpdate = 0,
                    Message = $"{status.SyncType} is current for cycle {currentCycleDate:yyyy-MM-dd}."
                };
            }

            // Stale — calculate days past cycle without update
            var daysPast = (now - currentCycleDate).TotalDays;

            string severity;
            string message;

            if (daysPast < 1)
            {
                severity = "info";
                message = $"{status.SyncType} has a new cycle ({currentCycleDate:yyyy-MM-dd}) but hasn't synced yet.";
            }
            else if (daysPast < 2)
            {
                severity = "warning";
                message = $"{status.SyncType} is {daysPast:F1} days past cycle {currentCycleDate:yyyy-MM-dd} without update.";
            }
            else
            {
                severity = "critical";
                message = $"{status.SyncType} is {daysPast:F1} days past cycle {currentCycleDate:yyyy-MM-dd} without update.";
            }

            return new DataFreshnessResult
            {
                SyncType = status.SyncType,
                IsFresh = false,
                Severity = severity,
                StalenessMode = "CycleBased",
                LastSuccessfulSync = status.LastSuccessfulSyncUtc,
                ConsecutiveFailures = status.ConsecutiveFailures,
                LastErrorMessage = status.LastErrorMessage,
                CurrentCycleDate = currentCycleDate,
                DaysPastCycleWithoutUpdate = Math.Round(daysPast, 1),
                Message = message
            };
        }
    }
}

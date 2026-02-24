using PreflightApi.Infrastructure.Dtos;

namespace PreflightApi.Infrastructure.Interfaces
{
    public interface IDataSyncStatusService
    {
        Task RecordSuccessAsync(string syncType, int recordCount = 0, CancellationToken ct = default);
        Task RecordFailureAsync(string syncType, string errorMessage, CancellationToken ct = default);
        Task<IReadOnlyList<DataFreshnessResult>> GetAllFreshnessAsync(CancellationToken ct = default);
    }
}

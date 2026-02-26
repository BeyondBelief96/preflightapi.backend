using PreflightApi.Infrastructure.Dtos;

namespace PreflightApi.Infrastructure.Interfaces
{
    public interface IDataSyncStatusService
    {
        Task RecordSuccessAsync(string syncType, int recordCount = 0, CancellationToken ct = default);
        Task RecordFailureAsync(string syncType, string errorMessage, CancellationToken ct = default);
        Task<IReadOnlyList<DataCurrencyResult>> GetAllCurrencyAsync(CancellationToken ct = default);
        Task UpdateAlertStateAsync(string syncType, string severity, CancellationToken ct = default);
        Task ClearAlertStateAsync(string syncType, CancellationToken ct = default);
    }
}

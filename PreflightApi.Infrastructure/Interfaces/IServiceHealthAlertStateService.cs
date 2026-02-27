using PreflightApi.Domain.Entities;

namespace PreflightApi.Infrastructure.Interfaces
{
    public interface IServiceHealthAlertStateService
    {
        Task<IReadOnlyList<ServiceHealthAlertState>> GetAllAsync(CancellationToken ct = default);
        Task UpsertStatusAsync(string serviceName, string status, CancellationToken ct = default);
        Task UpdateAlertStateAsync(string serviceName, string severity, CancellationToken ct = default);
        Task ClearAlertStateAsync(string serviceName, CancellationToken ct = default);
    }
}

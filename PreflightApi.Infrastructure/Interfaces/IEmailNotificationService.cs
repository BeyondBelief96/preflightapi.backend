using PreflightApi.Infrastructure.Dtos;

namespace PreflightApi.Infrastructure.Interfaces
{
    public interface IEmailNotificationService
    {
        Task SendStalenessAlertAsync(IReadOnlyList<DataCurrencyResult> staleTypes, CancellationToken ct = default);
        Task SendRecoveryNoticeAsync(IReadOnlyList<string> recoveredTypes, CancellationToken ct = default);
    }
}

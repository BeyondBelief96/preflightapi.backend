using PreflightApi.Infrastructure.Dtos;

namespace PreflightApi.Infrastructure.Interfaces
{
    public interface IEmailNotificationService
    {
        Task SendStalenessAlertAsync(IReadOnlyList<DataFreshnessResult> staleTypes, CancellationToken ct = default);
        Task SendRecoveryNoticeAsync(IReadOnlyList<string> recoveredTypes, CancellationToken ct = default);
    }
}

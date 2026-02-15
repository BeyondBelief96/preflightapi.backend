namespace PreflightApi.Infrastructure.Interfaces;

public interface IObstacleDailyChangeCronService
{
    Task ProcessDailyChangesAsync(CancellationToken cancellationToken = default);
}

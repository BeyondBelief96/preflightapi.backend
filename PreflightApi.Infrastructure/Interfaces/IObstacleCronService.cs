namespace PreflightApi.Infrastructure.Interfaces;

public interface IObstacleCronService
{
    Task DownloadAndProcessObstaclesAsync(CancellationToken cancellationToken = default);
}

namespace PreflightApi.Infrastructure.Interfaces;

public interface IFixCronService
{
    Task DownloadAndProcessDataAsync(CancellationToken cancellationToken = default);
}

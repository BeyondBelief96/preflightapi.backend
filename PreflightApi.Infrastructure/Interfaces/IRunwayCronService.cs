namespace PreflightApi.Infrastructure.Interfaces;

public interface IRunwayCronService
{
    Task DownloadAndProcessDataAsync(CancellationToken cancellationToken = default);
}

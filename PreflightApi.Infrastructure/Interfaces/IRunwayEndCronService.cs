namespace PreflightApi.Infrastructure.Interfaces;

public interface IRunwayEndCronService
{
    Task DownloadAndProcessDataAsync(CancellationToken cancellationToken = default);
    Task LinkRunwayEndsToRunwaysAsync(CancellationToken cancellationToken = default);
}

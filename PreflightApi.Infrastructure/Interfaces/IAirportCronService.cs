namespace PreflightApi.Infrastructure.Interfaces;

public interface IAirportCronService
{
    Task DownloadAndProcessDataAsync(CancellationToken cancellationToken = default);
}

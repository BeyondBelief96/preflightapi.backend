namespace PreflightApi.Infrastructure.Interfaces;

public interface IWeatherStationCronService
{
    Task DownloadAndProcessDataAsync(CancellationToken cancellationToken = default);
}

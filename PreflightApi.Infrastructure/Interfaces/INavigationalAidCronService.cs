namespace PreflightApi.Infrastructure.Interfaces;

public interface INavigationalAidCronService
{
    Task DownloadAndProcessDataAsync(CancellationToken cancellationToken = default);
}

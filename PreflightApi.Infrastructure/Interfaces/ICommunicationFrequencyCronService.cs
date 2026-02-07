namespace PreflightApi.Infrastructure.Interfaces;

public interface ICommunicationFrequencyCronService
{
    Task DownloadAndProcessDataAsync(CancellationToken cancellationToken = default);
}

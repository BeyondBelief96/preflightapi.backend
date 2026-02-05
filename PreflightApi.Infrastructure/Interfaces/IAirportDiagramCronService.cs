namespace PreflightApi.Infrastructure.Interfaces
{
    public interface IAirportDiagramCronService
    {
        Task DownloadAndProcessAirportDiagramsAsync(CancellationToken cancellationToken = default);
    }
}

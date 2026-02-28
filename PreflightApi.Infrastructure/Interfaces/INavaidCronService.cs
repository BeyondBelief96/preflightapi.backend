namespace PreflightApi.Infrastructure.Interfaces
{
    public interface INavaidCronService
    {
        Task DownloadAndProcessDataAsync(CancellationToken cancellationToken = default);
        Task ProcessCheckpointsAndRemarksAsync(CancellationToken cancellationToken = default);
    }
}

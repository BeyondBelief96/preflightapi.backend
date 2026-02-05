namespace PreflightApi.Infrastructure.Interfaces
{
    public interface IChartSupplementCronService
    {
        Task DownloadAndProcessChartSupplementsAsync(CancellationToken cancellationToken = default);
    }
}

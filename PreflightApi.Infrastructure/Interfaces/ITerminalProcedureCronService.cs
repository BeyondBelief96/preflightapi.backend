namespace PreflightApi.Infrastructure.Interfaces
{
    public interface ITerminalProcedureCronService
    {
        Task DownloadAndProcessTerminalProceduresAsync(CancellationToken cancellationToken = default);
    }
}

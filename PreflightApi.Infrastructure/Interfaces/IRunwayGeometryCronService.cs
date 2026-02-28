namespace PreflightApi.Infrastructure.Interfaces;

public interface IRunwayGeometryCronService
{
    Task UpdateRunwayGeometriesAsync(CancellationToken cancellationToken = default);
}

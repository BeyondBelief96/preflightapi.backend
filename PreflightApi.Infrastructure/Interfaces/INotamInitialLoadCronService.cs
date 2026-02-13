namespace PreflightApi.Infrastructure.Interfaces;

/// <summary>
/// Performs a full load of all NOTAMs by classification from the FAA NMS API.
/// Runs daily and on startup (if the database is empty) to ensure complete coverage.
/// </summary>
public interface INotamInitialLoadCronService
{
    Task LoadAllClassificationsAsync(CancellationToken ct = default);
}

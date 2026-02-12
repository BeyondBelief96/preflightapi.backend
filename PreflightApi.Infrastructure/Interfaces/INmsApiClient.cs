using PreflightApi.Infrastructure.Dtos.Notam;

namespace PreflightApi.Infrastructure.Interfaces;

/// <summary>
/// Low-level client for FAA NMS (NOTAM Management System) API
/// Handles OAuth2 authentication and HTTP calls
/// </summary>
public interface INmsApiClient
{
    /// <summary>
    /// Gets NOTAMs for a specific location (airport identifier)
    /// </summary>
    /// <param name="location">ICAO code or FAA identifier (e.g., KDFW, DFW)</param>
    /// <param name="filters">Optional NMS query filters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of NOTAMs for the location</returns>
    Task<List<NotamDto>> GetNotamsByLocationAsync(string location, NotamFilterDto? filters = null, CancellationToken ct = default);

    /// <summary>
    /// Gets NOTAMs within a radius of a geographic point
    /// </summary>
    /// <param name="lat">Latitude in decimal degrees</param>
    /// <param name="lon">Longitude in decimal degrees</param>
    /// <param name="radiusNm">Radius in nautical miles</param>
    /// <param name="filters">Optional NMS query filters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of NOTAMs within the radius</returns>
    Task<List<NotamDto>> GetNotamsByRadiusAsync(double lat, double lon, double radiusNm, NotamFilterDto? filters = null, CancellationToken ct = default);

    /// <summary>
    /// Gets a single NOTAM by its NMS ID
    /// </summary>
    /// <param name="nmsId">16-digit NMS identifier</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The NOTAM if found, null otherwise</returns>
    Task<NotamDto?> GetNotamByNmsIdAsync(string nmsId, CancellationToken ct = default);

    /// <summary>
    /// Gets NOTAMs created, updated, or canceled since a given timestamp (delta sync).
    /// Limited to a 24-hour window by the FAA.
    /// </summary>
    /// <param name="lastUpdatedDate">Fetch NOTAMs updated since this UTC timestamp</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of NOTAMs updated since the timestamp</returns>
    Task<List<NotamDto>> GetNotamsByLastUpdatedDateAsync(DateTime lastUpdatedDate, CancellationToken ct = default);

    /// <summary>
    /// Downloads all active NOTAMs across all classifications via the /v1/notams/il bulk endpoint.
    /// Returns a redirect to a compressed file containing the complete NOTAM dataset.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of all active NOTAMs</returns>
    Task<List<NotamDto>> GetAllNotamsInitialLoadAsync(CancellationToken ct = default);

}

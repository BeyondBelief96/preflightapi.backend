using PreflightApi.Infrastructure.Dtos.Notam;
using PreflightApi.Infrastructure.Dtos.Pagination;

namespace PreflightApi.Infrastructure.Interfaces;

/// <summary>
/// Business logic service for NOTAM queries with caching
/// </summary>
public interface INotamService
{
    /// <summary>
    /// Gets NOTAMs for a specific airport with caching
    /// </summary>
    /// <param name="icaoCodeOrIdent">ICAO code or FAA identifier (e.g., KDFW, DFW)</param>
    /// <param name="filters">Optional NMS query filters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Response containing NOTAMs for the airport</returns>
    Task<NotamResponseDto> GetNotamsForAirportAsync(string icaoCodeOrIdent, NotamFilterDto? filters = null, CancellationToken ct = default);

    /// <summary>
    /// Gets NOTAMs within a radius of a geographic point with caching
    /// </summary>
    /// <param name="lat">Latitude in decimal degrees</param>
    /// <param name="lon">Longitude in decimal degrees</param>
    /// <param name="radiusNm">Radius in nautical miles (max 100)</param>
    /// <param name="filters">Optional NMS query filters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Response containing NOTAMs within the radius</returns>
    Task<NotamResponseDto> GetNotamsByRadiusAsync(double lat, double lon, double radiusNm, NotamFilterDto? filters = null, CancellationToken ct = default);

    /// <summary>
    /// Gets NOTAMs for a flight route (multiple airports) with caching and deduplication
    /// </summary>
    /// <param name="request">Route query request with airport identifiers</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Response containing aggregated NOTAMs for the route</returns>
    Task<NotamResponseDto> GetNotamsForRouteAsync(NotamQueryByRouteRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets a single NOTAM by its NMS ID with caching
    /// </summary>
    /// <param name="nmsId">16-digit NMS identifier</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The NOTAM if found, null otherwise</returns>
    Task<NotamDto?> GetNotamByNmsIdAsync(string nmsId, CancellationToken ct = default);

    /// <summary>
    /// Searches NOTAMs across all locations using filter criteria with cursor-based pagination.
    /// At least one filter must be provided.
    /// </summary>
    /// <param name="filters">NMS query filters (at least one required)</param>
    /// <param name="cursor">Opaque cursor for pagination (null for first page)</param>
    /// <param name="limit">Maximum items per page</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated response containing matching NOTAMs</returns>
    Task<PaginatedResponse<NotamDto>> SearchNotamsAsync(NotamFilterDto filters, string? cursor = null, int limit = 100, CancellationToken ct = default);
}

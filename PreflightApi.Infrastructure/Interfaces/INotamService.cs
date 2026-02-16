using PreflightApi.Infrastructure.Dtos.Notam;
using PreflightApi.Infrastructure.Dtos.Pagination;

namespace PreflightApi.Infrastructure.Interfaces;

/// <summary>
/// Business logic service for NOTAM queries backed by the local PostgreSQL database.
/// Data is synced from the FAA NMS system via background cron jobs (delta every 3 min, full daily).
/// Expired and cancelled NOTAMs are periodically purged.
/// </summary>
public interface INotamService
{
    /// <summary>
    /// Gets active NOTAMs for a specific airport.
    /// Excludes expired and cancelled NOTAMs that have not yet been purged.
    /// </summary>
    /// <param name="icaoCodeOrIdent">ICAO code or FAA identifier (e.g., KDFW, DFW)</param>
    /// <param name="filters">Optional query filters (classification, feature, freeText, date range)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Response containing active NOTAMs for the airport</returns>
    Task<NotamResponseDto> GetNotamsForAirportAsync(string icaoCodeOrIdent, NotamFilterDto? filters = null, CancellationToken ct = default);

    /// <summary>
    /// Gets active NOTAMs within a radius of a geographic point via PostGIS spatial query.
    /// Excludes expired and cancelled NOTAMs that have not yet been purged.
    /// </summary>
    /// <param name="lat">Latitude in decimal degrees</param>
    /// <param name="lon">Longitude in decimal degrees</param>
    /// <param name="radiusNm">Radius in nautical miles (max 100)</param>
    /// <param name="filters">Optional query filters (classification, feature, freeText, date range)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Response containing active NOTAMs within the radius</returns>
    Task<NotamResponseDto> GetNotamsByRadiusAsync(double lat, double lon, double radiusNm, NotamFilterDto? filters = null, CancellationToken ct = default);

    /// <summary>
    /// Gets active NOTAMs for a flight route (airports and/or waypoints) with deduplication.
    /// Excludes expired and cancelled NOTAMs that have not yet been purged.
    /// </summary>
    /// <param name="request">Route query request with airport identifiers and/or route points</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Response containing aggregated, deduplicated NOTAMs for the route</returns>
    Task<NotamResponseDto> GetNotamsForRouteAsync(NotamQueryByRouteRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets a single NOTAM by its NMS ID.
    /// Does not apply active filters — may return recently expired or cancelled NOTAMs
    /// that have not yet been purged. This is not a historical archive.
    /// </summary>
    /// <param name="nmsId">Numeric NMS identifier</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The NOTAM if found, null otherwise</returns>
    Task<NotamDto?> GetNotamByNmsIdAsync(string nmsId, CancellationToken ct = default);

    /// <summary>
    /// Gets NOTAMs matching a NOTAM number in various formats (domestic, FDC, ICAO, bare number).
    /// Does not apply active filters — may return recently expired or cancelled NOTAMs
    /// that have not yet been purged. This is not a historical archive.
    /// </summary>
    /// <param name="notamNumber">NOTAM number in any supported format</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of matching NOTAMs (may be multiple for ambiguous bare numbers)</returns>
    Task<List<NotamDto>> GetNotamsByNumberAsync(string notamNumber, CancellationToken ct = default);

    /// <summary>
    /// Searches active NOTAMs across all locations using filter criteria with cursor-based pagination.
    /// At least one filter must be provided. Excludes expired and cancelled NOTAMs that have not yet been purged.
    /// </summary>
    /// <param name="filters">Query filters (at least one required)</param>
    /// <param name="cursor">Opaque cursor for pagination (null for first page)</param>
    /// <param name="limit">Maximum items per page</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated response containing matching active NOTAMs</returns>
    Task<PaginatedResponse<NotamDto>> SearchNotamsAsync(NotamFilterDto filters, string? cursor = null, int limit = 100, CancellationToken ct = default);
}

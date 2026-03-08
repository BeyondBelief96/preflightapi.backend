using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Navlog;
using PreflightApi.Infrastructure.Dtos.Pagination;

namespace PreflightApi.Infrastructure.Interfaces;

public interface IObstacleService
{
    Task<ObstacleDto?> GetByOasNumber(string oasNumber, CancellationToken ct = default);
    Task<IEnumerable<ObstacleDto>> GetByOasNumbers(IEnumerable<string> oasNumbers, CancellationToken ct = default);
    Task<PaginatedResponse<ObstacleDto>> SearchNearby(double latitude, double longitude, double radiusNm, int? minHeightAgl, string? cursor, int limit, CancellationToken ct = default);
    Task<PaginatedResponse<ObstacleDto>> GetByState(string stateCode, int? minHeightAgl, string? cursor, int limit, CancellationToken ct = default);
    Task<PaginatedResponse<ObstacleDto>> GetByBoundingBox(double minLat, double maxLat, double minLon, double maxLon, int? minHeightAgl, string? cursor, int limit, CancellationToken ct = default);

    /// <summary>
    /// Gets obstacles along a flight route using two methods:
    /// 1. Route corridor: obstacles within 10 NM of route with height >= cruising altitude - 2000 ft
    /// 2. Airport vicinity: ALL obstacles within 10 NM of airports on the route (regardless of height)
    /// </summary>
    Task<IReadOnlyCollection<string>> GetObstacleOasNumbersForRouteAsync(
        IEnumerable<WaypointDto> waypoints,
        int? cruisingAltitude = null,
        CancellationToken cancellationToken = default);
}

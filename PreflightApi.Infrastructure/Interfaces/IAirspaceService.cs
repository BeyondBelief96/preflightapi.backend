using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Navlog;
using PreflightApi.Infrastructure.Dtos.Pagination;

namespace PreflightApi.Infrastructure.Interfaces;

public interface IAirspaceService
{
    Task<PaginatedResponse<AirspaceDto>> GetByClasses(string[] airspaceClasses, string? cursor, int limit, CancellationToken ct = default);
    Task<PaginatedResponse<AirspaceDto>> GetByCities(string[] cities, string? cursor, int limit, CancellationToken ct = default);
    Task<PaginatedResponse<AirspaceDto>> GetByStates(string[] states, string? cursor, int limit, CancellationToken ct = default);
    Task<PaginatedResponse<SpecialUseAirspaceDto>> GetByTypeCodes(string[] typeCodes, string? cursor, int limit, CancellationToken ct = default);
    Task<IEnumerable<AirspaceDto>> GetByIcaoOrIdents(string[] icaoOrIdents, CancellationToken ct = default);
    Task<IEnumerable<AirspaceDto>> GetByGlobalIds(string[] globalIds, CancellationToken ct = default);
    Task<IEnumerable<SpecialUseAirspaceDto>> GetSpecialUseByGlobalIds(string[] globalIds, CancellationToken ct = default);
    Task<IReadOnlyCollection<string>> GetAirspaceGlobalIdsForRouteAsync(IEnumerable<WaypointDto> waypoints, CancellationToken ct = default);
    Task<IReadOnlyCollection<string>> GetSpecialUseAirspaceGlobalIdsForRouteAsync(IEnumerable<WaypointDto> waypoints, CancellationToken ct = default);
}

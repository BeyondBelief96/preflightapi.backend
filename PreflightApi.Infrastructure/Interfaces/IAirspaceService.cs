using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Navlog;
using PreflightApi.Infrastructure.Dtos.Pagination;

namespace PreflightApi.Infrastructure.Interfaces;

public interface IAirspaceService
{
    Task<PaginatedResponse<AirspaceDto>> GetByClasses(string[] airspaceClasses, string? cursor, int limit);
    Task<PaginatedResponse<AirspaceDto>> GetByCities(string[] cities, string? cursor, int limit);
    Task<PaginatedResponse<AirspaceDto>> GetByStates(string[] states, string? cursor, int limit);
    Task<PaginatedResponse<SpecialUseAirspaceDto>> GetByTypeCodes(string[] typeCodes, string? cursor, int limit);
    Task<IEnumerable<AirspaceDto>> GetByIcaoOrIdents(string[] icaoOrIdents);
    Task<IEnumerable<AirspaceDto>> GetByGlobalIds(string[] globalIds);
    Task<IEnumerable<SpecialUseAirspaceDto>> GetSpecialUseByGlobalIds(string[] globalIds);
    Task<IReadOnlyCollection<string>> GetAirspaceGlobalIdsForRouteAsync(IEnumerable<WaypointDto> waypoints, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<string>> GetSpecialUseAirspaceGlobalIdsForRouteAsync(IEnumerable<WaypointDto> waypoints, CancellationToken cancellationToken = default);
}

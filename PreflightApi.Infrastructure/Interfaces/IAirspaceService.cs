using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Navlog;

namespace PreflightApi.Infrastructure.Interfaces;

public interface IAirspaceService
{
    Task<IEnumerable<AirspaceDto>> GetByClasses(string[] airspaceClasses);
    Task<IEnumerable<AirspaceDto>> GetByCities(string[] cities);
    Task<IEnumerable<AirspaceDto>> GetByStates(string[] states);
    Task<IEnumerable<SpecialUseAirspaceDto>> GetByTypeCodes(string[] typeCodes);
    Task<IEnumerable<AirspaceDto>> GetByIcaoOrIdents(string[] icaoOrIdents);
    Task<IEnumerable<AirspaceDto>> GetByGlobalIds(string[] globalIds);
    Task<IEnumerable<SpecialUseAirspaceDto>> GetSpecialUseByGlobalIds(string[] globalIds);
    Task<IReadOnlyCollection<string>> GetAirspaceGlobalIdsForRouteAsync(IEnumerable<WaypointDto> waypoints, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<string>> GetSpecialUseAirspaceGlobalIdsForRouteAsync(IEnumerable<WaypointDto> waypoints, CancellationToken cancellationToken = default);
}
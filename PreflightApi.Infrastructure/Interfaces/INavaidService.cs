using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;

namespace PreflightApi.Infrastructure.Interfaces;

public interface INavaidService
{
    Task<PaginatedResponse<NavaidDto>> GetNavaids(string? search, string? navType, string? stateCode, string? cursor, int limit, CancellationToken ct = default);
    Task<IEnumerable<NavaidDto>> GetNavaidsByIdentifier(string navId, CancellationToken ct = default);
    Task<IEnumerable<NavaidDto>> GetNavaidsByIdentifiers(IEnumerable<string> navIds, CancellationToken ct = default);
    Task<PaginatedResponse<NavaidDto>> SearchNearby(double latitude, double longitude, double radiusNm, string? navType, string? cursor, int limit, CancellationToken ct = default);
}

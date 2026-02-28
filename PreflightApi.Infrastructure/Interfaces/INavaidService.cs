using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;

namespace PreflightApi.Infrastructure.Interfaces;

public interface INavaidService
{
    Task<PaginatedResponse<NavaidDto>> GetNavaids(string? search, string? navType, string? stateCode, string? cursor, int limit);
    Task<IEnumerable<NavaidDto>> GetNavaidsByIdentifier(string navId);
    Task<IEnumerable<NavaidDto>> GetNavaidsByIdentifiers(IEnumerable<string> navIds);
    Task<PaginatedResponse<NavaidDto>> SearchNearby(decimal latitude, decimal longitude, double radiusNm, string? navType, string? cursor, int limit);
}

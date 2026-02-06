using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;

namespace PreflightApi.Infrastructure.Interfaces
{
    public interface IMetarService
    {
        Task<MetarDto> GetMetarForAirport(string icaoIdOrIdent);
        Task<PaginatedResponse<MetarDto>> GetMetarsByState(string stateCode, string? cursor, int limit);
        Task<PaginatedResponse<MetarDto>> GetMetarsByStates(string[] stateCodes, string? cursor, int limit);
    }
}

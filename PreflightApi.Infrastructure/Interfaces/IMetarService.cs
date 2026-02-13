using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;

namespace PreflightApi.Infrastructure.Interfaces
{
    public interface IMetarService
    {
        Task<MetarDto> GetMetarForAirport(string icaoIdOrIdent);
        Task<PaginatedResponse<MetarDto>> GetMetarsByStates(string[] stateCodes, string? cursor, int limit);
    }
}

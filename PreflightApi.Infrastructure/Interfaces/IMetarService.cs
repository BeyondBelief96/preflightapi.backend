using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;

namespace PreflightApi.Infrastructure.Interfaces
{
    public interface IMetarService
    {
        Task<MetarDto> GetMetarForAirport(string icaoIdOrIdent, CancellationToken ct = default);
        Task<IEnumerable<MetarDto>> GetMetarsForAirports(string[] icaoCodesOrIdents, CancellationToken ct = default);
        Task<PaginatedResponse<MetarDto>> GetMetarsByStates(string[] stateCodes, string? cursor, int limit, CancellationToken ct = default);
    }
}

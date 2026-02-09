using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;

namespace PreflightApi.Infrastructure.Interfaces;

public interface IFixService
{
    Task<PaginatedResponse<FixDto>> GetAllAsync(string? search, string? cursor, int limit);
    Task<IEnumerable<FixDto>> GetByIdentifierAsync(string identifier);
    Task<PaginatedResponse<FixDto>> GetByStateAsync(string stateCode, string? cursor, int limit);
    Task<PaginatedResponse<FixDto>> GetByUseCodeAsync(string useCode, string? cursor, int limit);
}

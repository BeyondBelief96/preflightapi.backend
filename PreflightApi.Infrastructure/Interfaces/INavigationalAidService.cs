using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;

namespace PreflightApi.Infrastructure.Interfaces;

public interface INavigationalAidService
{
    Task<PaginatedResponse<NavigationalAidDto>> GetAllAsync(string? search, string? cursor, int limit);
    Task<IEnumerable<NavigationalAidDto>> GetByIdentifierAsync(string identifier);
    Task<PaginatedResponse<NavigationalAidDto>> GetByTypeAsync(string facilityType, string? cursor, int limit);
    Task<PaginatedResponse<NavigationalAidDto>> GetByStateAsync(string stateCode, string? cursor, int limit);
}

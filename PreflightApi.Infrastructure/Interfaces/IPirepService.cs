using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;

namespace PreflightApi.Infrastructure.Interfaces
{
    public interface IPirepService
    {
        Task<PaginatedResponse<PirepDto>> GetAllPireps(string? cursor, int limit, CancellationToken ct);
    }
}

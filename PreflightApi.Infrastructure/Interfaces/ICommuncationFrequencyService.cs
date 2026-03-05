using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;

namespace PreflightApi.Infrastructure.Interfaces;

public interface ICommunicationFrequencyService
{
    Task<PaginatedResponse<CommunicationFrequencyDto>> GetFrequenciesByServicedFacility(string servicedFacility, string? cursor, int limit, CancellationToken ct = default);
}

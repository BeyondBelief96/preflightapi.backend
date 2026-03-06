using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;

namespace PreflightApi.Infrastructure.Interfaces;

public interface IGAirmetService
{
    Task<PaginatedResponse<GAirmetDto>> GetAllGAirmets(string? cursor, int limit, CancellationToken ct);
    Task<PaginatedResponse<GAirmetDto>> GetGAirmetsByProduct(GAirmetProduct product, string? cursor, int limit, CancellationToken ct);
    Task<PaginatedResponse<GAirmetDto>> GetGAirmetsByHazardType(GAirmetHazardType hazardType, string? cursor, int limit, CancellationToken ct);
    Task<PaginatedResponse<GAirmetDto>> SearchAffecting(double latitude, double longitude, string? cursor, int limit, CancellationToken ct);
}

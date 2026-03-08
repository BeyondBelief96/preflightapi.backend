using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;

namespace PreflightApi.Infrastructure.Interfaces;

public interface ISigmetService
{
    Task<PaginatedResponse<SigmetDto>> GetAllSigmets(string? cursor, int limit, CancellationToken ct);
    Task<PaginatedResponse<SigmetDto>> GetSigmetsByHazardType(SigmetHazardType hazardType, string? cursor, int limit, CancellationToken ct);
    Task<PaginatedResponse<SigmetDto>> SearchAffecting(double latitude, double longitude, string? cursor, int limit, CancellationToken ct);
    Task<PaginatedResponse<SigmetDto>> SearchByArea(double minLat, double maxLat, double minLon, double maxLon, string? cursor, int limit, CancellationToken ct);
}

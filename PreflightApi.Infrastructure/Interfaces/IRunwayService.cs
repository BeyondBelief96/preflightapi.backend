using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;

namespace PreflightApi.Infrastructure.Interfaces;

public interface IRunwayService
{
    Task<IEnumerable<RunwayDto>> GetRunwaysByAirportAsync(string icaoCodeOrIdent, bool includeGeometry = false, CancellationToken ct = default);

    Task<PaginatedResponse<RunwayDto>> GetRunways(
        string? search,
        RunwaySurfaceType? surfaceType,
        int? minLength,
        string? state,
        bool? lighted,
        string? cursor,
        int limit,
        CancellationToken ct = default);

    Task<PaginatedResponse<RunwayDto>> SearchNearby(
        double latitude,
        double longitude,
        double radiusNm,
        int? minLength,
        RunwaySurfaceType? surfaceType,
        bool includeGeometry,
        string? cursor,
        int limit,
        CancellationToken ct = default);
}

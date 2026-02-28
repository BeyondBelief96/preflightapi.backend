using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;

namespace PreflightApi.Infrastructure.Interfaces;

public interface IRunwayService
{
    Task<IEnumerable<RunwayDto>> GetRunwaysByAirportAsync(string icaoCodeOrIdent, bool includeGeometry = false);

    Task<PaginatedResponse<RunwayDto>> GetRunways(
        string? search,
        RunwaySurfaceType? surfaceType,
        int? minLength,
        string? state,
        bool? lighted,
        string? cursor,
        int limit);

    Task<PaginatedResponse<RunwayDto>> SearchNearby(
        decimal latitude,
        decimal longitude,
        double radiusNm,
        int? minLength,
        RunwaySurfaceType? surfaceType,
        bool includeGeometry,
        string? cursor,
        int limit);
}

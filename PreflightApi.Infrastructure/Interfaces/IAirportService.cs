using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;

namespace PreflightApi.Infrastructure.Interfaces;

public interface IAirportService
{
    Task<PaginatedResponse<AirportDto>> GetAirports(string? search, string[]? stateCodes, string? cursor, int limit, CancellationToken ct = default);
    Task<AirportDto> GetAirportByIcaoCodeOrIdent(string icaoCodeOrIdent, CancellationToken ct = default);
    Task<IEnumerable<AirportDto>> GetAirportsByIcaoCodesOrIdents(string[] codesOrIdents, CancellationToken ct = default);
    Task<PaginatedResponse<AirportDto>> SearchNearby(double latitude, double longitude, double radiusNm, string? cursor, int limit, CancellationToken ct = default);
}

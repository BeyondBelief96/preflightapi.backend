using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;

namespace PreflightApi.Infrastructure.Interfaces;

public interface IAirportService
{
    Task<PaginatedResponse<AirportDto>> GetAirports(string? search, string[]? stateCodes, string? cursor, int limit);
    Task<AirportDto> GetAirportByIcaoCodeOrIdent(string icaoCodeOrIdent);
    Task<IEnumerable<AirportDto>> GetAirportsByIcaoCodesOrIdents(string[] codesOrIdents);
    Task<PaginatedResponse<AirportDto>> SearchNearby(decimal latitude, decimal longitude, double radiusNm, string? cursor, int limit);
}

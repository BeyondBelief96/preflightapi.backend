using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;

namespace PreflightApi.Infrastructure.Interfaces;

public interface IAirportService
{
    Task<PaginatedResponse<AirportDto>> GetAllAirports(string? search, string? cursor, int limit);
    Task<AirportDto> GetAirportByIcaoCodeOrIdent(string icaoCodeOrIdent);
    Task<PaginatedResponse<AirportDto>> GetAirportsByState(string stateCode, string? cursor, int limit);
    Task<PaginatedResponse<AirportDto>> GetAirportsByStates(string[] stateCodes, string? cursor, int limit);
    Task<IEnumerable<AirportDto>> GetAirportsByIcaoCodesOrIdents(string[] codesOrIdents);
    Task<PaginatedResponse<AirportDto>> GetAirportsByPrefix(string prefix, string? cursor, int limit);
    Task<IEnumerable<AirportDto>> SearchAirports(string query);
}

using PreflightApi.Infrastructure.Dtos;

namespace PreflightApi.Infrastructure.Interfaces;

public interface ITafService
{
    Task<TafDto> GetTafByIcaoCode(string icaoCodeOrIdent, CancellationToken ct = default);
    Task<IEnumerable<TafDto>> GetTafsForAirports(string[] icaoCodesOrIdents, CancellationToken ct = default);
}
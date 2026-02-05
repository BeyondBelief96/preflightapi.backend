using PreflightApi.Infrastructure.Dtos;

namespace PreflightApi.Infrastructure.Interfaces;

public interface ITafService
{
    Task<TafDto> GetTafByIcaoCode(string icaoCodeOrIdent);
}
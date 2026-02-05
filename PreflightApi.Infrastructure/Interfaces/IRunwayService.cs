using PreflightApi.Infrastructure.Dtos;

namespace PreflightApi.Infrastructure.Interfaces;

public interface IRunwayService
{
    Task<IEnumerable<RunwayDto>> GetRunwaysByAirportAsync(string icaoCodeOrIdent);
}

using PreflightApi.Infrastructure.Dtos;

namespace PreflightApi.Infrastructure.Interfaces;

public interface ITerminalProcedureService
{
    Task<TerminalProceduresResponseDto> GetTerminalProceduresByAirportCode(string airportCode, string? chartCode = null);
}

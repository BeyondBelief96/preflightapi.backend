using PreflightApi.Infrastructure.Dtos;

namespace PreflightApi.Infrastructure.Interfaces;

public interface IAirportDiagramService
{
    Task<AirportDiagramsResponseDto> GetAirportDiagramsByAirportCode(string airportCode);
}
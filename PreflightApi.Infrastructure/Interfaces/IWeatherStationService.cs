using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Pagination;

namespace PreflightApi.Infrastructure.Interfaces;

public interface IWeatherStationService
{
    Task<PaginatedResponse<WeatherStationDto>> GetAllAsync(string? search, string? cursor, int limit);
    Task<IEnumerable<WeatherStationDto>> GetByIdentifierAsync(string identifier);
    Task<PaginatedResponse<WeatherStationDto>> GetByTypeAsync(string sensorType, string? cursor, int limit);
    Task<PaginatedResponse<WeatherStationDto>> GetByStateAsync(string stateCode, string? cursor, int limit);
    Task<IEnumerable<WeatherStationDto>> GetByAirportAsync(string airportIdentifier);
}

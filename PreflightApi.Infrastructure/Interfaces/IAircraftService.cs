using PreflightApi.Infrastructure.Dtos.Aircraft;

namespace PreflightApi.Infrastructure.Interfaces;

public interface IAircraftService
{
    Task<AircraftDto> CreateAircraft(string userId, CreateAircraftRequestDto request);
    Task<AircraftDto> UpdateAircraft(string userId, string id, UpdateAircraftRequestDto request);
    Task<AircraftDto?> GetAircraft(string userId, string id);
    Task<List<AircraftDto>> GetAircraftByUserId(string userId);
    Task DeleteAircraft(string userId, string id);
}

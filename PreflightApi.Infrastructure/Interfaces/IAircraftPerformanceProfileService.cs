using PreflightApi.Infrastructure.Dtos.AircraftPerformanceProfiles;

namespace PreflightApi.Infrastructure.Interfaces;

public interface IAircraftPerformanceProfileService
{
    Task<AircraftPerformanceProfileDto> SaveProfile(SaveAircraftPerformanceProfileRequestDto request);
    Task<AircraftPerformanceProfileDto> UpdateProfile(string id, UpdateAircraftPerformanceProfileRequestDto request);
    Task<List<AircraftPerformanceProfileDto>> GetProfilesByUserId(string userId);
    Task DeleteProfile(string userId, string profileId);
}
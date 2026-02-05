using PreflightApi.Infrastructure.Dtos.Navlog;

namespace PreflightApi.Infrastructure.Interfaces;

public interface INavlogService
{
    Task<NavlogResponseDto> CalculateNavlog(NavlogRequestDto request);
    Task<BearingAndDistanceResponseDto> CalculateBearingAndDistance(BearingAndDistanceRequestDto request);
    Task<WindsAloftDto> GetWindsAloftData(int forecast);
}
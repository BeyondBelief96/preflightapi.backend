using PreflightApi.Infrastructure.Dtos.Navlog;

namespace PreflightApi.Infrastructure.Interfaces;

public interface INavlogService
{
    Task<NavlogResponseDto> CalculateNavlog(NavlogRequestDto request, CancellationToken ct = default);
    Task<BearingAndDistanceResponseDto> CalculateBearingAndDistance(BearingAndDistanceRequestDto request, CancellationToken ct = default);
    Task<WindsAloftDto> GetWindsAloftData(int forecast, CancellationToken ct = default);
}

using PreflightApi.Infrastructure.Dtos.Navlog;

namespace PreflightApi.Infrastructure.Interfaces;

public interface IWindsAloftService
{
    Task<WindsAloftDto> FetchWindsAloftData(int fcstHours, CancellationToken ct = default);
}
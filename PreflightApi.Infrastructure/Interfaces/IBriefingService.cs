using PreflightApi.Infrastructure.Dtos.Briefing;

namespace PreflightApi.Infrastructure.Interfaces;

public interface IBriefingService
{
    Task<RouteBriefingResponse> GetRouteBriefingAsync(RouteBriefingRequest request, CancellationToken ct = default);
}

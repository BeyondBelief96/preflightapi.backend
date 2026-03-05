using PreflightApi.Infrastructure.Dtos;

namespace PreflightApi.Infrastructure.Interfaces;

public interface IChartSupplementService
{
    Task<ChartSupplementsResponseDto> GetChartSupplementsByAirportCode(string icaoCodeOrIdent, CancellationToken ct = default);
}

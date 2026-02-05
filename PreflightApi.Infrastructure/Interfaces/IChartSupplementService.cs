using PreflightApi.Infrastructure.Dtos;

namespace PreflightApi.Infrastructure.Interfaces;

public interface IChartSupplementService
{
    Task<ChartSupplementUrlDto> GetChartSupplementUrlByAirportCode(string icaoCodeOrIdent);
}
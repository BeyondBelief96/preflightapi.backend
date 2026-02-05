using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Dtos;

namespace PreflightApi.Infrastructure.Interfaces;

public interface IGAirmetService
{
    Task<IEnumerable<GAirmetDto>> GetAllGAirmets();
    Task<IEnumerable<GAirmetDto>> GetGAirmetsByProduct(GAirmetProduct product);
    Task<IEnumerable<GAirmetDto>> GetGAirmetsByHazardType(GAirmetHazardType hazardType);
}

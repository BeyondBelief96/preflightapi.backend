using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Dtos;

namespace PreflightApi.Infrastructure.Interfaces;

public interface IGAirmetService
{
    Task<List<GAirmetDto>> GetAllGAirmets();
    Task<List<GAirmetDto>> GetGAirmetsByProduct(GAirmetProduct product);
    Task<List<GAirmetDto>> GetGAirmetsByHazardType(GAirmetHazardType hazardType);
}

using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Dtos;

namespace PreflightApi.Infrastructure.Interfaces;

public interface IAirsigmetService
{
    Task<List<AirsigmetDto>> GetAllAirsigmets();
    Task<List<AirsigmetDto>> GetAirsigmetsByHazardType(AirsigmetHazardType hazardType);
}

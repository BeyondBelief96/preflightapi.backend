using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Dtos;

namespace PreflightApi.Infrastructure.Interfaces;

public interface IAirsigmetService
{
    Task<IEnumerable<AirsigmetDto>> GetAllAirsigmets();
    Task<IEnumerable<AirsigmetDto>> GetAirsigmetsByHazardType(AirsigmetHazardType hazardType);
}
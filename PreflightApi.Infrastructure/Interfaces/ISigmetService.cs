using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Dtos;

namespace PreflightApi.Infrastructure.Interfaces;

public interface ISigmetService
{
    Task<List<SigmetDto>> GetAllSigmets();
    Task<List<SigmetDto>> GetSigmetsByHazardType(SigmetHazardType hazardType);
}

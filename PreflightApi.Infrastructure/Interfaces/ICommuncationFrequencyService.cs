using PreflightApi.Infrastructure.Dtos;

namespace PreflightApi.Infrastructure.Interfaces;

public interface ICommunicationFrequencyService
{
    Task<IEnumerable<CommunicationFrequencyDto>> GetFrequenciesByServicedFacility(string servicedFacility);
}
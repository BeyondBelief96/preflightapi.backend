using PreflightApi.Infrastructure.Dtos;

namespace PreflightApi.Infrastructure.Interfaces
{
    public interface IMetarService
    {
        Task<MetarDto> GetMetarForAirport(string icaoIdOrIdent);
        Task<IEnumerable<MetarDto>> GetMetarsByState(string stateCode);
        Task<IEnumerable<MetarDto>> GetMetarsByStates(string[] stateCodes);
    }
}

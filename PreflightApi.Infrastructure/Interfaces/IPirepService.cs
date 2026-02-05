using PreflightApi.Infrastructure.Dtos;

namespace PreflightApi.Infrastructure.Interfaces
{
    public interface IPirepService
    {
        Task<IEnumerable<PirepDto>> GetAllPireps();
    }
}

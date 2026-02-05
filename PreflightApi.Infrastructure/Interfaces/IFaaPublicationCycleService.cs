using PreflightApi.Domain.Entities;
using PreflightApi.Domain.ValueObjects.FaaPublications;

namespace PreflightApi.Infrastructure.Interfaces
{
    public interface IFaaPublicationCycleService
    {
        Task<FaaPublicationCycle?> GetPublicationCycleAsync(PublicationType type);
        Task<bool> ShouldRunUpdateAsync(PublicationType publicationType, DateTime currentDate);
        Task UpdateLastSuccessfulRunAsync(PublicationType publicationType, DateTime updateDate);
    }
}

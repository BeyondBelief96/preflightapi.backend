using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Domain.ValueObjects.FaaPublications;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Infrastructure.Services
{
    public class FaaPublicationCycleService : IFaaPublicationCycleService
    {
        private readonly PreflightApiDbContext _dbContext;
        private readonly ILogger<FaaPublicationCycleService> _logger;

        public FaaPublicationCycleService(PreflightApiDbContext dbContext, ILogger<FaaPublicationCycleService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<FaaPublicationCycle?> GetPublicationCycleAsync(PublicationType type)
        {
                var publicationCycle = await _dbContext.FaaPublicationCycles
                    .FirstOrDefaultAsync(p => p.PublicationType == type);

                return publicationCycle ?? throw new NotFoundException("FaaPublicationCycle", type);
            
        }

        public async Task<bool> ShouldRunUpdateAsync(PublicationType publicationType, DateTime currentDate)
        {
            var publicationCycle = await _dbContext.FaaPublicationCycles.AsNoTracking().FirstOrDefaultAsync(c => c.PublicationType == publicationType);

            if(publicationCycle == null)
            {
                _logger.LogError("No publication cycle found for type: {PublicationType}", publicationType);
                return false;
            }

            // If there has never been a successful update, we should run the update.
            if (!publicationCycle.LastSuccessfulUpdate.HasValue)
            {
                return true;
            }

            var daysSinceKnownValidDate = (currentDate - publicationCycle.KnownValidDate).TotalDays;
            var completeCycles = Math.Floor(daysSinceKnownValidDate / publicationCycle.CycleLengthDays);
            var mostRecentCycleDate = publicationCycle.KnownValidDate.AddDays(completeCycles * publicationCycle.CycleLengthDays);
            var nextCycleDate = mostRecentCycleDate.AddDays(publicationCycle.CycleLengthDays);

            if(currentDate >= mostRecentCycleDate && currentDate < nextCycleDate && 
                (publicationCycle.LastSuccessfulUpdate == null || publicationCycle.LastSuccessfulUpdate < mostRecentCycleDate))
            {
                return true;
            }

            return false;
        }

        public async Task UpdateLastSuccessfulRunAsync(PublicationType publicationType, DateTime updateDate)
        {
            var cycle = await _dbContext.FaaPublicationCycles.FirstOrDefaultAsync(c => c.PublicationType.Equals(publicationType));

            if (cycle != null)
            {
                cycle.LastSuccessfulUpdate = updateDate;
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}

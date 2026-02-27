using Microsoft.EntityFrameworkCore;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Infrastructure.Services
{
    public class ServiceHealthAlertStateService : IServiceHealthAlertStateService
    {
        private readonly PreflightApiDbContext _dbContext;

        public ServiceHealthAlertStateService(PreflightApiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<ServiceHealthAlertState>> GetAllAsync(CancellationToken ct = default)
        {
            return await _dbContext.ServiceHealthAlertStates
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task UpsertStatusAsync(string serviceName, string status, CancellationToken ct = default)
        {
            var existing = await _dbContext.ServiceHealthAlertStates.FindAsync([serviceName], ct);

            if (existing == null)
            {
                _dbContext.ServiceHealthAlertStates.Add(new ServiceHealthAlertState
                {
                    ServiceName = serviceName,
                    LastKnownStatus = status,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.LastKnownStatus = status;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task UpdateAlertStateAsync(string serviceName, string severity, CancellationToken ct = default)
        {
            var existing = await _dbContext.ServiceHealthAlertStates.FindAsync([serviceName], ct);

            if (existing != null)
            {
                existing.LastAlertSentUtc = DateTime.UtcNow;
                existing.LastAlertSeverity = severity;
                await _dbContext.SaveChangesAsync(ct);
            }
        }

        public async Task ClearAlertStateAsync(string serviceName, CancellationToken ct = default)
        {
            var existing = await _dbContext.ServiceHealthAlertStates.FindAsync([serviceName], ct);

            if (existing != null)
            {
                existing.LastAlertSentUtc = null;
                existing.LastAlertSeverity = null;
                await _dbContext.SaveChangesAsync(ct);
            }
        }
    }
}

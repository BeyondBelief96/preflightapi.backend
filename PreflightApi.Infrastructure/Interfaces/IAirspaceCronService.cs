namespace PreflightApi.Infrastructure.Interfaces
{
    public interface IAirspaceCronService<TEntity>
    {
        Task UpdateAirspacesAsync(CancellationToken cancellationToken = default);
    }
}

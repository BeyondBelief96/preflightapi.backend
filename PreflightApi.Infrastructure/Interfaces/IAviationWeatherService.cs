namespace PreflightApi.Infrastructure.Interfaces
{
    public interface IAviationWeatherService<T>
    {
        Task PollWeatherDataAsync(CancellationToken cancellationToken = default);
    }
}

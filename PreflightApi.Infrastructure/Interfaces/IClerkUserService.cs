namespace PreflightApi.Infrastructure.Interfaces
{
    public interface IClerkUserService
    {
        Task<IReadOnlyList<string>> GetAllUserEmailsAsync(CancellationToken ct = default);
    }
}

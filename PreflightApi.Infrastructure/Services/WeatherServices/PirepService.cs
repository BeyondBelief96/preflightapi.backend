using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Mappers;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.Services.WeatherServices;

public class PirepService : IPirepService
{
    private readonly PreflightApiDbContext _context;
    private readonly ILogger<PirepService> _logger;

    public PirepService(
        PreflightApiDbContext context,
        ILogger<PirepService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PaginatedResponse<PirepDto>> GetAllPireps(string? cursor, int limit, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Retrieving PIREPs, cursor: {Cursor}, limit: {Limit}", cursor, limit);

            var query = _context.Pireps.AsNoTracking();
            return await query.ToPaginatedAsync(p => p.Id, PirepMapper.ToDto, cursor, limit, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all PIREPs");
            throw;
        }
    }
}

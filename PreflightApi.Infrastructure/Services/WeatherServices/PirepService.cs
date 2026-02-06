using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Mappers;
using PreflightApi.Infrastructure.Interfaces;

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

    public async Task<List<PirepDto>> GetAllPireps()
    {
        try
        {
            _logger.LogInformation("Retrieving all PIREPs");

            var pireps = await _context.Pireps.ToListAsync();
            return pireps.Select(PirepMapper.ToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all PIREPs");
            throw;
        }
    }
}

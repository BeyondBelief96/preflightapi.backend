using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Mappers;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.Services.WeatherServices
{
    public class MetarService : IMetarService
    {
        private readonly PreflightApiDbContext _context;
        private readonly ILogger<MetarService> _logger;

        public MetarService(
            PreflightApiDbContext context,
            ILogger<MetarService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<MetarDto> GetMetarForAirport(string icaoIdOrIdent)
        {
            try
            {
                _logger.LogInformation("Retrieving METAR for airport identifier: {IcaoIdOrIdent}", icaoIdOrIdent);

                if (string.IsNullOrWhiteSpace(icaoIdOrIdent))
                {
                    throw new ArgumentException("Airport identifier cannot be null or empty", nameof(icaoIdOrIdent));
                }

                var metar = await _context.Metars
                    .FirstOrDefaultAsync(m => m.StationId == icaoIdOrIdent.ToUpperInvariant());

                if (metar == null)
                {
                    _logger.LogDebug("METAR not found directly, searching for airport: {IcaoIdOrIdent}", icaoIdOrIdent);

                    var airport = await _context.Airports
                        .FirstOrDefaultAsync(a => a.ArptId == icaoIdOrIdent.ToUpperInvariant() ||
                                                a.IcaoId == icaoIdOrIdent.ToUpperInvariant());

                    if (airport == null)
                    {
                        _logger.LogWarning("Airport not found for identifier: {IcaoIdOrIdent}", icaoIdOrIdent);
                        throw new AirportNotFoundException(icaoIdOrIdent);
                    }

                    var modifiedIdent = airport.StateCode switch
                    {
                        "AK" or "HI" => $"P{airport.ArptId}",
                        _ => $"K{airport.ArptId}"
                    };

                    _logger.LogDebug("Searching for METAR with modified identifier: {ModifiedIdent}", modifiedIdent);
                    metar = await _context.Metars
                        .FirstOrDefaultAsync(m => m.StationId == modifiedIdent);
                }

                if (metar == null)
                {
                    _logger.LogWarning("METAR not found for airport: {IcaoIdOrIdent}", icaoIdOrIdent);
                    throw new MetarNotFoundException(icaoIdOrIdent);
                }

                _logger.LogInformation("Successfully retrieved METAR for airport: {IcaoIdOrIdent}", icaoIdOrIdent);
                return MetarMapper.ToDto(metar);
            }
            catch (Exception ex) when (ex is not DomainException)
            {
                _logger.LogError(ex, "Error retrieving METAR for airport: {IcaoIdOrIdent}", icaoIdOrIdent);
                throw;
            }
        }

        public async Task<PaginatedResponse<MetarDto>> GetMetarsByStates(string[] stateCodes, string? cursor = null, int limit = 100)
        {
            try
            {
                _logger.LogInformation("Retrieving METARs for states: {@StateCodes}, cursor: {Cursor}, limit: {Limit}",
                    stateCodes, cursor, limit);

                if (stateCodes == null || !stateCodes.Any())
                {
                    throw new ArgumentException("State codes array cannot be null or empty", nameof(stateCodes));
                }

                if (stateCodes.Any(string.IsNullOrWhiteSpace))
                {
                    throw new ArgumentException("State codes cannot contain null or empty values", nameof(stateCodes));
                }

                var upperStateCodes = stateCodes.Select(s => s?.ToUpperInvariant())
                    .Where(s => s != null)
                    .ToHashSet();

                var query = _context.Metars
                    .Where(m => _context.Airports.Any(a =>
                        a.StateCode != null &&
                        upperStateCodes.Contains(a.StateCode) &&
                        (a.IcaoId == m.StationId ||
                         (m.StationId != null &&
                          m.StationId.Length > 1 &&
                          (m.StationId.StartsWith("K") || m.StationId.StartsWith("P")) &&
                          a.ArptId == m.StationId.Substring(1)) ||
                         a.ArptId == m.StationId)));

                return await query.ToPaginatedAsync(m => m.Id, MetarMapper.ToDto, cursor, limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving METARs for states: {@StateCodes}", stateCodes);
                throw;
            }
        }
    }
}

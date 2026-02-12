using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Mappers;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.Services
{
    public class AirportService : IAirportService
    {
        private readonly PreflightApiDbContext _context;
        private readonly ILogger<AirportService> _logger;

        public AirportService(
            PreflightApiDbContext context,
            ILogger<AirportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PaginatedResponse<AirportDto>> GetAirports(string? search = null, string[]? stateCodes = null, string? cursor = null, int limit = 100)
        {
            try
            {
                _logger.LogInformation("Getting airports with search: {Search}, states: {States}, cursor: {Cursor}, limit: {Limit}",
                    search, stateCodes != null ? string.Join(",", stateCodes) : null, cursor, limit);

                var query = _context.Airports.AsQueryable();

                if (stateCodes is { Length: > 0 })
                {
                    var upperStateCodes = stateCodes.Select(s => s.ToUpperInvariant()).ToArray();
                    query = query.Where(a => a.StateCode != null && upperStateCodes.Contains(a.StateCode));
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchUpper = search.ToUpperInvariant();
                    query = query.Where(a =>
                        (a.IcaoId != null && a.IcaoId.ToUpperInvariant().StartsWith(searchUpper)) ||
                        (a.ArptId != null && a.ArptId.ToUpperInvariant().StartsWith(searchUpper)) ||
                        (a.ArptName != null && a.ArptName.ToUpperInvariant().Contains(searchUpper)) ||
                        (a.City != null && a.City.ToUpperInvariant().Contains(searchUpper)));
                }

                return await query.ToPaginatedAsync(a => a.SiteNo, AirportMapper.ToDto, cursor, limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting airports with search: {Search}, states: {States}", search,
                    stateCodes != null ? string.Join(",", stateCodes) : null);
                throw;
            }
        }

        public async Task<AirportDto> GetAirportByIcaoCodeOrIdent(string icaoCodeOrIdent)
        {
            try
            {
                _logger.LogInformation("Getting airport by ICAO code or ident: {IcaoCodeOrIdent}", icaoCodeOrIdent);

                var airport = await _context.Airports
                    .FirstOrDefaultAsync(a =>
                        a.IcaoId == icaoCodeOrIdent.ToUpperInvariant() ||
                        a.ArptId == icaoCodeOrIdent.ToUpperInvariant());

                if (airport == null)
                {
                    throw new AirportNotFoundException(icaoCodeOrIdent);
                }

                return AirportMapper.ToDto(airport);
            }
            catch (Exception ex) when (ex is not AirportNotFoundException)
            {
                _logger.LogError(ex, "Error getting airport by ICAO code or ident: {IcaoCodeOrIdent}", icaoCodeOrIdent);
                throw;
            }
        }

        public async Task<IEnumerable<AirportDto>> GetAirportsByIcaoCodesOrIdents(string[] codesOrIdents)
        {
            try
            {
                _logger.LogInformation("Getting airports by ICAO codes or idents: {CodesOrIdents}",
                    string.Join(", ", codesOrIdents));

                var upperCodes = codesOrIdents.Select(c => c.ToUpperInvariant()).ToArray();
                var airports = await _context.Airports
                    .Where(a =>
                        (a.IcaoId != null && upperCodes.Contains(a.IcaoId)) ||
                        (a.ArptId != null && upperCodes.Contains(a.ArptId)))
                    .ToListAsync();

                return airports.Select(AirportMapper.ToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting airports by ICAO codes or idents: {CodesOrIdents}",
                    string.Join(", ", codesOrIdents));
                throw;
            }
        }
    }
}

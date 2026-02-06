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

        public async Task<PaginatedResponse<AirportDto>> GetAllAirports(string? search = null, string? cursor = null, int limit = 100)
        {
            try
            {
                _logger.LogInformation("Getting all airports with search: {Search}, cursor: {Cursor}, limit: {Limit}",
                    search, cursor, limit);

                var query = _context.Airports.AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToUpperInvariant();
                    query = query.Where(a =>
                        (a.IcaoId != null && a.IcaoId.ToUpperInvariant().Contains(search)) ||
                        (a.ArptId != null && a.ArptId.ToUpperInvariant().Contains(search))
                    );
                }

                return await query.ToPaginatedAsync(a => a.SiteNo, AirportMapper.ToDto, cursor, limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all airports with search: {Search}", search);
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

        public async Task<PaginatedResponse<AirportDto>> GetAirportsByState(string stateCode, string? cursor = null, int limit = 100)
        {
            try
            {
                _logger.LogInformation("Getting airports by state: {StateCode}, cursor: {Cursor}, limit: {Limit}",
                    stateCode, cursor, limit);

                var query = _context.Airports
                    .Where(a => a.StateCode == stateCode.ToUpperInvariant());

                return await query.ToPaginatedAsync(a => a.SiteNo, AirportMapper.ToDto, cursor, limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting airports by state: {StateCode}", stateCode);
                throw;
            }
        }

        public async Task<PaginatedResponse<AirportDto>> GetAirportsByStates(string[] stateCodes, string? cursor = null, int limit = 100)
        {
            try
            {
                _logger.LogInformation("Getting airports by states: {StateCodes}, cursor: {Cursor}, limit: {Limit}",
                    string.Join(", ", stateCodes), cursor, limit);

                var upperStateCodes = stateCodes.Select(s => s.ToUpperInvariant()).ToArray();
                var query = _context.Airports
                    .Where(a => a.StateCode != null && upperStateCodes.Contains(a.StateCode));

                return await query.ToPaginatedAsync(a => a.SiteNo, AirportMapper.ToDto, cursor, limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting airports by states: {StateCodes}", string.Join(", ", stateCodes));
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

        public async Task<PaginatedResponse<AirportDto>> GetAirportsByPrefix(string prefix, string? cursor = null, int limit = 100)
        {
            try
            {
                _logger.LogInformation("Getting airports by prefix: {Prefix}, cursor: {Cursor}, limit: {Limit}",
                    prefix, cursor, limit);

                if (string.IsNullOrWhiteSpace(prefix))
                {
                    return new PaginatedResponse<AirportDto>
                    {
                        Data = Enumerable.Empty<AirportDto>(),
                        Pagination = new PaginationMetadata
                        {
                            Limit = limit,
                            NextCursor = null,
                            HasMore = false
                        }
                    };
                }

                var upperPrefix = prefix.ToUpperInvariant();
                var query = _context.Airports
                    .Where(a =>
                        (a.IcaoId != null && a.IcaoId.ToUpperInvariant().StartsWith(upperPrefix)) ||
                        (a.ArptId != null && a.ArptId.ToUpperInvariant().StartsWith(upperPrefix)));

                return await query.ToPaginatedAsync(a => a.SiteNo, AirportMapper.ToDto, cursor, limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting airports by prefix: {Prefix}", prefix);
                throw;
            }
        }

        public async Task<IEnumerable<AirportDto>> SearchAirports(string query)
        {
            try
            {
                _logger.LogInformation("Searching airports with query: {Query}", query);

                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                {
                    return Enumerable.Empty<AirportDto>();
                }

                var queryLower = query.ToLower();

                var airports = await _context.Airports
                    .Where(a =>
                        (a.IcaoId != null && a.IcaoId.ToLower().StartsWith(queryLower)) ||
                        (a.ArptId != null && a.ArptId.ToLower().StartsWith(queryLower)) ||
                        (a.ArptName != null && a.ArptName.ToLower().Contains(queryLower)) ||
                        (a.City != null && a.City.ToLower().Contains(queryLower)))
                    .OrderByDescending(a =>
                        (a.IcaoId != null && a.IcaoId.ToLower().StartsWith(queryLower)) ||
                        (a.ArptId != null && a.ArptId.ToLower().StartsWith(queryLower)))
                    .ThenBy(a => a.ArptName)
                    .Take(50)
                    .ToListAsync();

                return airports.Select(AirportMapper.ToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching airports with query: {Query}", query);
                throw;
            }
        }
    }
}

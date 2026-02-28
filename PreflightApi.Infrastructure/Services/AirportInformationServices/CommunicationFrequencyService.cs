using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Mappers;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.Services.AirportInformationServices
{
    public class CommunicationFrequencyService : ICommunicationFrequencyService
    {
        private readonly PreflightApiDbContext _context;
        private readonly ILogger<CommunicationFrequencyService> _logger;

        public CommunicationFrequencyService(
            PreflightApiDbContext context,
            ILogger<CommunicationFrequencyService> logger)
        {
            _context = context;
            _logger = logger;
        }

        private string StripIcaoPrefix(string facilityCode)
        {
            facilityCode = facilityCode.ToUpperInvariant();

            // Return original code if it's 3 or fewer characters
            if (facilityCode.Length <= 3)
                return facilityCode;

            // If it starts with K, P, or H and is 4 characters long,
            // return just the last 3 characters
            if (facilityCode.Length == 4 &&
                (facilityCode.StartsWith("K") ||
                 facilityCode.StartsWith("P") ||
                 facilityCode.StartsWith("H")))
            {
                return facilityCode.Substring(1);
            }

            return facilityCode;
        }

        public async Task<PaginatedResponse<CommunicationFrequencyDto>> GetFrequenciesByServicedFacility(
            string servicedFacility,
            string? cursor = null,
            int limit = 100)
        {
            try
            {
                _logger.LogInformation("Getting frequencies for serviced facility code: {ServicedFacility}, cursor: {Cursor}, limit: {Limit}",
                    servicedFacility, cursor, limit);

                var strippedCode = StripIcaoPrefix(servicedFacility);

                _logger.LogDebug("Stripped facility code for lookup: {StrippedCode} (original: {OriginalCode})",
                    strippedCode, servicedFacility);

                // Verify the airport/facility exists
                var airportExists = await _context.Airports
                    .AnyAsync(a => a.ArptId == strippedCode || a.IcaoId == strippedCode || a.IcaoId == servicedFacility.ToUpperInvariant());

                if (!airportExists)
                {
                    throw new AirportNotFoundException(servicedFacility);
                }

                var query = _context.CommunicationFrequencies
                    .AsNoTracking()
                    .Where(f => f.ServicedFacility == strippedCode);

                var result = await query.ToPaginatedAsync(
                    f => f.Id, CommunicationFrequencyMapper.ToDto, cursor, limit);

                _logger.LogInformation("Found {Count} frequencies for serviced facility: {ServicedFacility}, hasMore: {HasMore}",
                    result.Data.Count(), servicedFacility, result.Pagination.HasMore);

                return result;
            }
            catch (Exception ex) when (ex is not AirportNotFoundException)
            {
                _logger.LogError(ex, "Error getting frequencies for serviced facility: {ServicedFacility}",
                    servicedFacility);
                throw;
            }
        }
    }
}

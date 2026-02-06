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

                // Decode cursor if provided
                var decodedCursor = CursorHelper.DecodeString(cursor);
                var query = _context.CommunicationFrequencies
                    .Where(f => f.ServicedFacility == strippedCode);

                // Apply cursor filter if decoded cursor is a valid Guid
                if (decodedCursor != null && Guid.TryParse(decodedCursor, out var cursorGuid))
                {
                    query = query.Where(f => f.Id.CompareTo(cursorGuid) > 0);
                }

                // Order by Id for pagination
                query = query.OrderBy(f => f.Id);

                // Fetch limit + 1 to determine if there are more results
                var items = await query.Take(limit + 1).ToListAsync();

                var hasMore = items.Count > limit;
                if (hasMore)
                {
                    items = items.Take(limit).ToList();
                }

                var data = items.Select(CommunicationFrequencyMapper.ToDto);
                var nextCursor = hasMore && items.Count > 0
                    ? CursorHelper.Encode(items[^1].Id.ToString())
                    : null;

                _logger.LogInformation("Found {Count} frequencies for serviced facility: {ServicedFacility}, hasMore: {HasMore}",
                    items.Count, servicedFacility, hasMore);

                return new PaginatedResponse<CommunicationFrequencyDto>
                {
                    Data = data,
                    Pagination = new PaginationMetadata
                    {
                        Limit = limit,
                        NextCursor = nextCursor,
                        HasMore = hasMore
                    }
                };
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

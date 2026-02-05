using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Mappers;
using PreflightApi.Infrastructure.Interfaces;

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
            facilityCode = facilityCode.ToUpper();
            
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

        public async Task<IEnumerable<CommunicationFrequencyDto>> GetFrequenciesByServicedFacility(string servicedFacility)
        {
            try
            {
                _logger.LogInformation("Getting frequencies for serviced facility code: {ServicedFacility}", 
                    servicedFacility);

                var strippedCode = StripIcaoPrefix(servicedFacility);
                
                _logger.LogDebug("Stripped facility code for lookup: {StrippedCode} (original: {OriginalCode})", 
                    strippedCode, servicedFacility);

                var frequencies = await _context.CommunicationFrequencies
                    .Where(f => f.ServicedFacility == strippedCode)
                    .OrderBy(f => f.FacilityType)
                    .ThenBy(f => f.Frequency)
                    .ToListAsync();

                if (!frequencies.Any())
                {
                    _logger.LogWarning("No frequencies found for serviced facility: {ServicedFacility} (stripped: {StrippedCode})", 
                        servicedFacility, strippedCode);
                }
                else
                {
                    _logger.LogInformation("Found {Count} frequencies for serviced facility: {ServicedFacility}", 
                        frequencies.Count, servicedFacility);
                }

                return frequencies.Select(CommunicationFrequencyMapper.ToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting frequencies for serviced facility: {ServicedFacility}", 
                    servicedFacility);
                throw;
            }
        }
    }
}
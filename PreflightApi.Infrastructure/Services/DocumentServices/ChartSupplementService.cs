using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Settings;
using PreflightApi.Domain.Exceptions;

namespace PreflightApi.Infrastructure.Services.DocumentServices;

public class ChartSupplementService : IChartSupplementService
{
    private readonly PreflightApiDbContext _context;
    private readonly ICloudStorageService _cloudStorageService;
    private readonly ILogger<ChartSupplementService> _logger;
    private readonly string _containerName;

    public ChartSupplementService(
        PreflightApiDbContext context,
        ICloudStorageService cloudStorageService,
        IOptions<CloudStorageSettings> cloudStorageSettings,
        ILogger<ChartSupplementService> logger)
    {
        _context = context;
        _cloudStorageService = cloudStorageService;
        _logger = logger;
        _containerName = cloudStorageSettings.Value.ChartSupplementsContainerName
            ?? throw new InvalidOperationException("CloudStorage:ChartSupplementsContainerName not configured");
    }

    private string StripIcaoPrefix(string airportCode)
    {
        airportCode = airportCode.ToUpperInvariant();

        if (airportCode.Length <= 3)
            return airportCode;

        if (airportCode.Length == 4 &&
            (airportCode.StartsWith("K") ||
             airportCode.StartsWith("P") ||
             airportCode.StartsWith("H")))
        {
            return airportCode[1..];
        }

        return airportCode;
    }

    /// <summary>
    /// The format of chart supplement keys in blob storage has the file name capitalized and extension lowercase.
    /// </summary>
    private string TransformFileName(string fileName)
    {
        var nameParts = fileName.Split('.');
        if (nameParts.Length != 2)
        {
            return fileName.ToUpperInvariant();
        }

        return $"{nameParts[0].ToUpperInvariant()}.{nameParts[1].ToLower()}";
    }

    public async Task<ChartSupplementsResponseDto> GetChartSupplementsByAirportCode(string airportCode)
    {
        var strippedCode = StripIcaoPrefix(airportCode);
        _logger.LogInformation("Searching for chart supplements with code: {StrippedCode} (original: {OriginalCode})",
            strippedCode, airportCode);

        var chartSupplements = await _context.ChartSupplements
            .Where(c => c.AirportCode == strippedCode)
            .ToListAsync();

        if (chartSupplements.Count == 0)
        {
            throw new ChartSupplementNotFoundException(airportCode);
        }

        var firstSupplement = chartSupplements.First();
        var supplements = new List<ChartSupplementDto>();

        foreach (var chartSupplement in chartSupplements)
        {
            if (string.IsNullOrEmpty(chartSupplement.FileName))
            {
                continue;
            }

            try
            {
                var transformedFileName = TransformFileName(chartSupplement.FileName);
                var presignedUrl = await _cloudStorageService.GeneratePresignedUrlAsync(
                    _containerName,
                    transformedFileName,
                    TimeSpan.FromHours(1));

                supplements.Add(new ChartSupplementDto
                {
                    PdfUrl = presignedUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating pre-signed URL for chart supplement: {FileName}", chartSupplement.FileName);
                // Continue with other supplements even if one fails
            }
        }

        if (supplements.Count == 0)
        {
            throw new ChartSupplementNotFoundException(airportCode);
        }

        return new ChartSupplementsResponseDto
        {
            AirportName = firstSupplement.AirportName,
            AirportCity = firstSupplement.AirportCity,
            AirportCode = firstSupplement.AirportCode,
            Supplements = supplements
        };
    }
}

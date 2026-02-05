using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Settings;
using PreflightApi.Domain.Exceptions;

namespace PreflightApi.Infrastructure.Services.DocumentServices;

public class AirportDiagramService : IAirportDiagramService
{
    private readonly PreflightApiDbContext _context;
    private readonly ICloudStorageService _cloudStorageService;
    private readonly ILogger<AirportDiagramService> _logger;
    private readonly string _containerName;

    public AirportDiagramService(
        PreflightApiDbContext context,
        ICloudStorageService cloudStorageService,
        IOptions<CloudStorageSettings> cloudStorageSettings,
        ILogger<AirportDiagramService> logger)
    {
        _context = context;
        _cloudStorageService = cloudStorageService;
        _logger = logger;
        _containerName = cloudStorageSettings.Value.AirportDiagramsContainerName
            ?? throw new InvalidOperationException("CloudStorage:AirportDiagramsContainerName not configured");
    }

    public async Task<AirportDiagramsResponseDto> GetAirportDiagramsByAirportCode(string airportCode)
    {
        var upperCode = airportCode.ToUpper();
        var airportDiagrams = await _context.AirportDiagrams
            .Where(d => d.IcaoIdent == upperCode || d.AirportIdent == upperCode)
            .ToListAsync();

        if (airportDiagrams.Count == 0)
        {
            throw new ResourceNotFoundException($"Airport diagrams not found for airport with code: {airportCode}");
        }

        var firstDiagram = airportDiagrams.First();
        var diagrams = new List<AirportDiagramDto>();

        foreach (var diagram in airportDiagrams)
        {
            try
            {
                var transformedFileName = diagram.FileName.ToUpper();
                var presignedUrl = await _cloudStorageService.GeneratePresignedUrlAsync(
                    _containerName,
                    transformedFileName,
                    TimeSpan.FromHours(1));

                diagrams.Add(new AirportDiagramDto
                {
                    ChartName = diagram.ChartName ?? "AIRPORT DIAGRAM",
                    PdfUrl = presignedUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating pre-signed URL for airport diagram: {FileName}", diagram.FileName);
                // Continue with other diagrams even if one fails
            }
        }

        if (diagrams.Count == 0)
        {
            throw new ResourceNotFoundException($"Could not generate URLs for airport diagrams: {airportCode}");
        }

        return new AirportDiagramsResponseDto
        {
            AirportName = firstDiagram.AirportName,
            IcaoIdent = firstDiagram.IcaoIdent,
            AirportIdent = firstDiagram.AirportIdent,
            Diagrams = diagrams
        };
    }
}
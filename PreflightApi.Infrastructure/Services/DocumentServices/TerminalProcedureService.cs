using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Settings;
using PreflightApi.Infrastructure.Utilities;
using PreflightApi.Domain.Exceptions;

namespace PreflightApi.Infrastructure.Services.DocumentServices;

public class TerminalProcedureService : ITerminalProcedureService
{
    private static readonly Dictionary<string, TerminalProcedureChartCode> ChartCodeMap = new()
    {
        ["IAP"] = TerminalProcedureChartCode.IAP,
        ["DP"] = TerminalProcedureChartCode.DP,
        ["STAR"] = TerminalProcedureChartCode.STAR,
        ["APD"] = TerminalProcedureChartCode.APD,
        ["MIN"] = TerminalProcedureChartCode.MIN,
        ["HOT"] = TerminalProcedureChartCode.HOT,
        ["DAU"] = TerminalProcedureChartCode.DAU,
        ["LAH"] = TerminalProcedureChartCode.LAH,
        ["ODP"] = TerminalProcedureChartCode.ODP
    };

    private readonly PreflightApiDbContext _context;
    private readonly ICloudStorageService _cloudStorageService;
    private readonly ILogger<TerminalProcedureService> _logger;
    private readonly string _containerName;

    public TerminalProcedureService(
        PreflightApiDbContext context,
        ICloudStorageService cloudStorageService,
        IOptions<CloudStorageSettings> cloudStorageSettings,
        ILogger<TerminalProcedureService> logger)
    {
        _context = context;
        _cloudStorageService = cloudStorageService;
        _logger = logger;
        _containerName = cloudStorageSettings.Value.TerminalProceduresContainerName
            ?? throw new InvalidOperationException("CloudStorage:TerminalProceduresContainerName not configured");
    }

    public async Task<TerminalProceduresResponseDto> GetTerminalProceduresByAirportCode(string airportCode, string? chartCode = null, CancellationToken ct = default)
    {
        var upperCode = airportCode.ToUpperInvariant();
        var query = _context.TerminalProcedures
            .Where(tp => tp.IcaoIdent == upperCode || tp.AirportIdent == upperCode);

        if (!string.IsNullOrWhiteSpace(chartCode))
        {
            var upperChartCode = chartCode.ToUpperInvariant();
            query = query.Where(tp => tp.ChartCode == upperChartCode);
        }

        var procedures = await query.ToListAsync(ct);

        if (procedures.Count == 0)
        {
            throw new TerminalProcedureNotFoundException(airportCode);
        }

        var firstProcedure = procedures.First();
        var result = new List<TerminalProcedureDto>();

        // Cache SAS URLs by PdfFileName to avoid duplicate generation for shared PDFs
        var urlCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var procedure in procedures)
        {
            try
            {
                var blobName = procedure.PdfFileName.ToUpperInvariant();

                if (!urlCache.TryGetValue(blobName, out var presignedUrl))
                {
                    presignedUrl = await _cloudStorageService.GeneratePresignedUrlAsync(
                        _containerName,
                        blobName,
                        TimeSpan.FromHours(1));
                    urlCache[blobName] = presignedUrl;
                }

                result.Add(new TerminalProcedureDto
                {
                    ChartCode = EnumParseHelper.Parse(procedure.ChartCode, _logger, nameof(procedure.ChartCode), "TerminalProcedure", procedure.PdfFileName, ChartCodeMap),
                    ChartName = procedure.ChartName,
                    PdfUrl = presignedUrl,
                    AmendmentNumber = procedure.AmendmentNumber,
                    AmendmentDate = procedure.AmendmentDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating pre-signed URL for terminal procedure: {PdfFileName}", procedure.PdfFileName);
            }
        }

        if (result.Count == 0)
        {
            throw new TerminalProcedureNotFoundException(airportCode);
        }

        return new TerminalProceduresResponseDto
        {
            AirportName = firstProcedure.AirportName,
            IcaoIdent = firstProcedure.IcaoIdent,
            AirportIdent = firstProcedure.AirportIdent,
            Procedures = result
        };
    }
}

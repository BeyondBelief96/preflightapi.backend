using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.ValueObjects.FaaPublications;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Enums;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Services.CronJobServices.NasrServices.Mappings;

namespace PreflightApi.Infrastructure.Services.CronJobServices.NasrServices;

public class RunwayEndCronService : FaaNasrBaseService<RunwayEnd>, IRunwayEndCronService
{
    private readonly PreflightApiDbContext _dbContext;
    private readonly ILogger<RunwayEndCronService> _logger;

    protected override NasrDataType DataType => NasrDataType.APT;
    protected override string[] UniqueIdentifiers => new[] { "SiteNo", "RunwayIdRef", "RunwayEndId" };
    protected override IEnumerable<(string FileName, Type ClassMap, bool IsBaseData)> CsvMappings =>
        new[]
        {
            ("APT_RWY_END.csv", typeof(RunwayEndMap), true),
        };

    protected override bool UsesLegacySiteNoDeduplication => false;

    protected override PublicationType PublicationType => PublicationType.NasrSubscription_Airport;

    public RunwayEndCronService(
        ILogger<RunwayEndCronService> logger,
        IHttpClientFactory httpClientFactory,
        IFaaPublicationCycleService faaPublicationCycleService,
        PreflightApiDbContext dbContext,
        ISyncTelemetryService telemetry)
        : base(logger, httpClientFactory, faaPublicationCycleService, dbContext, telemetry)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Links RunwayEnd entities to their parent Runway entities after data sync.
    /// This should be called after both RunwayCronService and RunwayEndCronService have completed.
    /// </summary>
    public async Task LinkRunwayEndsToRunwaysAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting to link runway ends to runways...");

        try
        {
            // Use raw SQL for efficient bulk update
            var sql = @"
                UPDATE runway_ends re
                SET runway_fk = r.""Id""
                FROM runways r
                WHERE re.site_no = r.site_no
                  AND re.runway_id_ref = r.runway_id
                  AND re.runway_fk IS NULL";

            var updatedCount = await _dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
            _logger.LogInformation("Linked {Count} runway ends to their parent runways", updatedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking runway ends to runways");
            throw;
        }
    }
}

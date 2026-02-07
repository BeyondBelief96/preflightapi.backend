using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.ValueObjects.FaaPublications;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Enums;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Services.CronJobServices.NasrServices.Mappings;

namespace PreflightApi.Infrastructure.Services.CronJobServices.NasrServices;

public class RunwayCronService : FaaNasrBaseService<Runway>, IRunwayCronService
{
    protected override NasrDataType DataType => NasrDataType.APT;
    protected override string[] UniqueIdentifiers => new[] { "SiteNo", "RunwayId" };
    protected override IEnumerable<(string FileName, Type ClassMap, bool IsBaseData)> CsvMappings =>
        new[]
        {
            ("APT_RWY.csv", typeof(RunwayMap), true),
        };

    protected override bool UsesLegacySiteNoDeduplication => false;

    protected override PublicationType PublicationType => PublicationType.NasrSubscription_Airport;

    public RunwayCronService(
        ILogger<RunwayCronService> logger,
        IHttpClientFactory httpClientFactory,
        IFaaPublicationCycleService faaPublicationCycleService,
        PreflightApiDbContext dbContext)
        : base(logger, httpClientFactory, faaPublicationCycleService, dbContext)
    {
    }
}

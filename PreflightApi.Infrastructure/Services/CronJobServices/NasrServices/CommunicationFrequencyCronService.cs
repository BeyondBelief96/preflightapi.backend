using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.ValueObjects.FaaPublications;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Enums;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Services.CronJobServices.NasrServices.Mappings;

namespace PreflightApi.Infrastructure.Services.CronJobServices.NasrServices
{
    public class CommunicationFrequencyCronService : FaaNasrBaseService<CommunicationFrequency>
    {
        protected override NasrDataType DataType => NasrDataType.FRQ;
        protected override string[] UniqueIdentifiers => new[] {
            "FacilityCode",
            "ServicedFacility",
            "ServicedSiteType",
            "ServicedState",
            "Frequency",
            "FrequencyUse",
            "Sectorization"
        };
        protected override IEnumerable<(string FileName, Type ClassMap, bool IsBaseData)> CsvMappings =>
        new[]
        {
            ("FRQ.csv", typeof(FrequencyMap), true),
        };

        protected override bool UsesLegacySiteNoDeduplication => false;

        protected override PublicationType PublicationType => PublicationType.NasrSubscription_Frequencies;

        public CommunicationFrequencyCronService(
            ILogger<CommunicationFrequencyCronService> logger,
            IHttpClientFactory httpClientFactory,
            IFaaPublicationCycleService faaPublicationCycleService,
            PreflightApiDbContext dbContext)
            : base(logger, httpClientFactory, faaPublicationCycleService, dbContext)
        {
        }
    }
}

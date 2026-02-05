using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.ValueObjects.FaaPublications;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Enums;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Services.CronJobServices.NasrServices.Mappings;

namespace PreflightApi.Infrastructure.Services.CronJobServices.NasrServices
{
    public class AirportCronService : FaaNasrBaseService<Airport>
    {
        protected override NasrDataType DataType => NasrDataType.APT;
        protected override string[] UniqueIdentifiers => new[] { "SiteNo" };
        protected override PublicationType PublicationType => PublicationType.NasrSubscription_Airport;
        protected override IEnumerable<(string FileName, Type ClassMap, bool IsBaseData)> CsvMappings =>
        new[]
        {
            ("APT_BASE.csv", typeof(AirportBaseMap), true),
            ("APT_ATT.csv", typeof(AirportAttendanceMap), false),
            ("APT_CON.csv", typeof(AirportContactMap), false)
        };

        public AirportCronService(
            ILogger<AirportCronService> logger,
            IHttpClientFactory httpClientFactory,
            IFaaPublicationCycleService faaPublicationCycleService,
            PreflightApiDbContext dbContext)
            : base(logger, httpClientFactory, faaPublicationCycleService, dbContext)
        {
        }
    }
}
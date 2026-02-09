using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.ValueObjects.FaaPublications;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Enums;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Services.CronJobServices.NasrServices.Mappings;

namespace PreflightApi.Infrastructure.Services.CronJobServices.NasrServices;

public class WeatherStationCronService : FaaNasrBaseService<WeatherStation>, IWeatherStationCronService
{
    protected override NasrDataType DataType => NasrDataType.AWOS;
    protected override string[] UniqueIdentifiers => new[] { "AsosAwosId", "AsosAwosType" };
    protected override PublicationType PublicationType => PublicationType.NasrSubscription_WeatherStations;
    protected override IEnumerable<(string FileName, Type ClassMap, bool IsBaseData)> CsvMappings =>
    new[]
    {
        ("AWOS.csv", typeof(WeatherStationMap), true),
    };

    protected override bool UsesLegacySiteNoDeduplication => false;

    public WeatherStationCronService(
        ILogger<WeatherStationCronService> logger,
        IHttpClientFactory httpClientFactory,
        IFaaPublicationCycleService faaPublicationCycleService,
        PreflightApiDbContext dbContext)
        : base(logger, httpClientFactory, faaPublicationCycleService, dbContext)
    {
    }
}

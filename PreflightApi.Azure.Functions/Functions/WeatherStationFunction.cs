using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.ValueObjects.FaaPublications;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Azure.Functions.Functions;

public class WeatherStationFunction
{
    private readonly IWeatherStationCronService _weatherStationService;
    private readonly IFaaPublicationCycleService _publicationService;
    private readonly ILogger<WeatherStationFunction> _logger;

    public WeatherStationFunction(
        IWeatherStationCronService weatherStationService,
        IFaaPublicationCycleService publicationService,
        ILoggerFactory loggerFactory)
    {
        _weatherStationService = weatherStationService ?? throw new ArgumentNullException(nameof(weatherStationService));
        _publicationService = publicationService ?? throw new ArgumentNullException(nameof(publicationService));
        _logger = loggerFactory.CreateLogger<WeatherStationFunction>();
    }

    [Function("WeatherStationFunction")]
    public async Task Run([TimerTrigger("0 0 4 * * *", RunOnStartup = false)] TimerInfo myTimer, FunctionContext context)
    {
        _logger.LogInformation("WeatherStation Function executed at: {Time}", DateTime.UtcNow);
        var cancellationToken = context.CancellationToken;

        try
        {
            var currentDate = DateTime.UtcNow;

            if (await _publicationService.ShouldRunUpdateAsync(PublicationType.NasrSubscription_WeatherStations, currentDate))
            {
                var sw = Stopwatch.StartNew();
                _logger.LogInformation("Starting weather station data update process");
                await _weatherStationService.DownloadAndProcessDataAsync(cancellationToken);
                await _publicationService.UpdateLastSuccessfulRunAsync(PublicationType.NasrSubscription_WeatherStations, currentDate);
                _logger.LogInformation("Weather station data update completed successfully in {ElapsedMs}ms", sw.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogInformation("No weather station data update needed at this time");
            }
        }
        catch (Exception)
        {
            throw;
        }
    }
}

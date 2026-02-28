using System.Text.Json.Serialization;

namespace PreflightApi.Infrastructure.Services.CronJobServices.ArcGisServices.Models;

public class RunwayArcGisModel
{
    [JsonPropertyName("OBJECTID")]
    public int ObjectId { get; set; }

    [JsonPropertyName("AIRPORT_ID")]
    public string? AirportId { get; set; }

    [JsonPropertyName("DESIGNATOR")]
    public string? Designator { get; set; }
}

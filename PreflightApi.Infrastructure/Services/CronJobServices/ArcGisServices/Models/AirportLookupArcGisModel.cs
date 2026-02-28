using System.Text.Json.Serialization;

namespace PreflightApi.Infrastructure.Services.CronJobServices.ArcGisServices.Models;

public class AirportLookupArcGisModel
{
    [JsonPropertyName("GLOBAL_ID")]
    public string? GlobalId { get; set; }

    [JsonPropertyName("IDENT")]
    public string? Ident { get; set; }
}

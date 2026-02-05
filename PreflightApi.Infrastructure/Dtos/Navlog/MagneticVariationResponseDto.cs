using System.Text.Json.Serialization;
using PreflightApi.Infrastructure.Services;

namespace PreflightApi.Infrastructure.Dtos.Navlog;


public class MagneticVariationResponseDto   
{
    [JsonPropertyName("result")]
    public MagneticVariationResultDto[]? Result { get; set; }
}
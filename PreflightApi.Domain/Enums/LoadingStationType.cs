using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LoadingStationType
{
    Standard,   // Weight entered directly in pounds/kg
    Fuel,       // Capacity in gallons, weight per gallon
    Oil         // Capacity in quarts, weight per quart
}

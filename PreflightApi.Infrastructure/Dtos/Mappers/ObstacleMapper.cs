using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.Dtos.Mappers;

public static class ObstacleMapper
{
    private static readonly Dictionary<string, ObstacleLighting> LightingMap = new()
    {
        ["R"] = ObstacleLighting.Red,
        ["D"] = ObstacleLighting.DualMediumWhiteStrobeRed,
        ["H"] = ObstacleLighting.HighIntensityWhiteStrobeRed,
        ["M"] = ObstacleLighting.MediumIntensityWhiteStrobe,
        ["S"] = ObstacleLighting.HighIntensityWhiteStrobe,
        ["F"] = ObstacleLighting.Flood,
        ["C"] = ObstacleLighting.DualMediumCatenary,
        ["W"] = ObstacleLighting.SynchronizedRedLighting,
        ["L"] = ObstacleLighting.Lighted,
        ["N"] = ObstacleLighting.None,
        ["U"] = ObstacleLighting.Unknown
    };

    private static readonly Dictionary<string, HorizontalAccuracy> HorizontalAccuracyMap = new()
    {
        ["1"] = HorizontalAccuracy.Within20Feet,
        ["2"] = HorizontalAccuracy.Within50Feet,
        ["3"] = HorizontalAccuracy.Within100Feet,
        ["4"] = HorizontalAccuracy.Within250Feet,
        ["5"] = HorizontalAccuracy.Within500Feet,
        ["6"] = HorizontalAccuracy.Within1000Feet,
        ["7"] = HorizontalAccuracy.WithinHalfNauticalMile,
        ["8"] = HorizontalAccuracy.Within1NauticalMile,
        ["9"] = HorizontalAccuracy.Unknown
    };

    private static readonly Dictionary<string, VerticalAccuracy> VerticalAccuracyMap = new()
    {
        ["A"] = VerticalAccuracy.Within3Feet,
        ["B"] = VerticalAccuracy.Within10Feet,
        ["C"] = VerticalAccuracy.Within20Feet,
        ["D"] = VerticalAccuracy.Within50Feet,
        ["E"] = VerticalAccuracy.Within125Feet,
        ["F"] = VerticalAccuracy.Within250Feet,
        ["G"] = VerticalAccuracy.Within500Feet,
        ["H"] = VerticalAccuracy.Within1000Feet,
        ["I"] = VerticalAccuracy.Unknown
    };

    private static readonly Dictionary<string, ObstacleMarking> MarkingMap = new()
    {
        ["P"] = ObstacleMarking.OrangeOrOrangeWhitePaint,
        ["W"] = ObstacleMarking.WhitePaintOnly,
        ["M"] = ObstacleMarking.Marked,
        ["F"] = ObstacleMarking.FlagMarker,
        ["S"] = ObstacleMarking.SphericalMarker,
        ["N"] = ObstacleMarking.None,
        ["U"] = ObstacleMarking.Unknown
    };

    private static readonly Dictionary<string, VerificationStatus> VerificationStatusMap = new()
    {
        ["O"] = VerificationStatus.Verified,
        ["U"] = VerificationStatus.Unverified
    };

    public static ObstacleDto ToDto(Obstacle obstacle, ILogger logger)
    {
        var id = obstacle.OasNumber;

        return new ObstacleDto
        {
            OasNumber = obstacle.OasNumber,
            StateId = obstacle.StateId,
            CityName = obstacle.CityName?.Trim(),
            Latitude = (double?)obstacle.LatDecimal,
            Longitude = (double?)obstacle.LongDecimal,
            ObstacleType = obstacle.ObstacleType?.Trim(),
            Quantity = obstacle.Quantity,
            HeightAgl = obstacle.HeightAgl,
            HeightAmsl = obstacle.HeightAmsl,
            Lighting = EnumParseHelper.Parse(obstacle.Lighting, logger, nameof(obstacle.Lighting), nameof(Obstacle), id, LightingMap),
            HorizontalAccuracy = EnumParseHelper.Parse(obstacle.HorizontalAccuracy, logger, nameof(obstacle.HorizontalAccuracy), nameof(Obstacle), id, HorizontalAccuracyMap),
            VerticalAccuracy = EnumParseHelper.Parse(obstacle.VerticalAccuracy, logger, nameof(obstacle.VerticalAccuracy), nameof(Obstacle), id, VerticalAccuracyMap),
            Marking = EnumParseHelper.Parse(obstacle.MarkIndicator, logger, nameof(obstacle.MarkIndicator), nameof(Obstacle), id, MarkingMap),
            VerificationStatus = EnumParseHelper.Parse(obstacle.VerificationStatus, logger, nameof(obstacle.VerificationStatus), nameof(Obstacle), id, VerificationStatusMap)
        };
    }
}

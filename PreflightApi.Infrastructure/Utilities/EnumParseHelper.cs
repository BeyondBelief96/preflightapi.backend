using Microsoft.Extensions.Logging;

namespace PreflightApi.Infrastructure.Utilities;

public static class EnumParseHelper
{
    public static TEnum? Parse<TEnum>(
        string? value,
        ILogger logger,
        string fieldName,
        string entityType,
        string entityId,
        Dictionary<string, TEnum> mapping) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim().ToUpperInvariant();

        if (mapping.TryGetValue(normalized, out var result))
            return result;

        logger.LogWarning(
            "Unrecognized {FieldName} value: '{RawValue}' for {EntityType} {EntityId}",
            fieldName, value, entityType, entityId);

        return null;
    }
}

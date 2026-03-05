using System.Text.Json;
using System.Text.Json.Serialization;

namespace PreflightApi.Infrastructure.Utilities;

/// <summary>
/// JSON converter that safely deserializes string values to nullable enums.
/// Returns null for unrecognized values instead of throwing JsonException.
/// Serializes enum values as their string names.
/// </summary>
public class SafeEnumJsonConverter<TEnum> : JsonConverter<TEnum?> where TEnum : struct, Enum
{
    public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var result))
                return result;

            // Unrecognized value — return null instead of throwing
            return null;
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            // Handle numeric enum values
            var intValue = reader.GetInt32();
            if (Enum.IsDefined(typeof(TEnum), intValue))
                return (TEnum)Enum.ToObject(typeof(TEnum), intValue);

            return null;
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, TEnum? value, JsonSerializerOptions options)
    {
        if (value == null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value.Value.ToString());
    }
}

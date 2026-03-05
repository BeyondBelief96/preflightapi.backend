using System.Text.Json;
using System.Text.Json.Serialization;

namespace PreflightApi.Infrastructure.Utilities;

/// <summary>
/// JSON converter that handles string-encoded booleans ("true"/"false") from the NMS API.
/// Reads both JSON string literals ("true") and JSON boolean literals (true).
/// Returns null for unrecognized or missing values.
/// </summary>
public class StringBoolJsonConverter : JsonConverter<bool?>
{
    public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.String => reader.GetString()?.Trim().ToUpperInvariant() switch
            {
                "TRUE" => true,
                "FALSE" => false,
                _ => null
            },
            _ => null
        };
    }

    public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
    {
        if (value == null)
            writer.WriteNullValue();
        else
            writer.WriteBooleanValue(value.Value);
    }
}

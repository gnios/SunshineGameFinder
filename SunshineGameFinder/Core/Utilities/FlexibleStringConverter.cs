using System.Text.Json;
using System.Text.Json.Serialization;

namespace SunshineGameFinder.Core.Utilities;

public class FlexibleStringConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.True => "true",
            JsonTokenType.False => "false",
            JsonTokenType.Number => reader.GetInt32().ToString(),
            JsonTokenType.Null => null,
            _ => throw new JsonException($"Unexpected token type: {reader.TokenType}")
        };
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        if (bool.TryParse(value, out var boolValue))
        {
            writer.WriteBooleanValue(boolValue);
        }
        else if (int.TryParse(value, out var intValue))
        {
            writer.WriteNumberValue(intValue);
        }
        else
        {
            writer.WriteStringValue(value);
        }
    }
}

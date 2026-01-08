using System.Text.Json;
using System.Text.Json.Serialization;
using SunshineGameFinder.Core.Models;

namespace SunshineGameFinder.Core.Utilities;

public class EnvironmentConverter : JsonConverter<Env>
{
    public override Env Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new Env();
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var jsonDoc = JsonDocument.ParseValue(ref reader);
            var root = jsonDoc.RootElement;

            return new Env
            {
                Path = root.TryGetProperty("PATH", out var pathElement) ? pathElement.GetString() : null
            };
        }

        throw new JsonException($"Unexpected token type: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, Env value, JsonSerializerOptions options)
    {
        if (string.IsNullOrEmpty(value.Path))
        {
            writer.WriteStringValue(string.Empty);
            return;
        }

        writer.WriteStartObject();
        if (!string.IsNullOrEmpty(value.Path))
        {
            writer.WriteString("PATH", value.Path);
        }
        writer.WriteEndObject();
    }
}

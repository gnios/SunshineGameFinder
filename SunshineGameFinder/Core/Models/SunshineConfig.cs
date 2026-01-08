using System.Text.Json.Serialization;
using SunshineGameFinder.Core.Utilities;

namespace SunshineGameFinder.Core.Models;

[JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(SunshineConfig))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}

[JsonConverter(typeof(EnvironmentConverter))]
public class Env
{
    private string? path;

    [JsonPropertyName("PATH")]
    public string? Path
    {
        get => path;
        set => path = value != null ? PathFormatter.FormatEnvPath(value) : null;
    }
}

public class SunshineConfig
{
    [JsonPropertyName("env")]
    public Env Env { get; set; } = new Env();

    [JsonPropertyName("apps")]
    public List<SunshineApp>? Apps { get; set; }
}

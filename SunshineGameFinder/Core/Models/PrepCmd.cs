using System.Text.Json.Serialization;
using SunshineGameFinder.Core.Utilities;

namespace SunshineGameFinder.Core.Models;

public class PrepCmd
{
    [JsonPropertyName("do")]
    public string? Do { get; set; }

    [JsonPropertyName("undo")]
    public string? Undo { get; set; }

    [JsonPropertyName("elevated")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Elevated { get; set; }
}

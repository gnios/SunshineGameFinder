using System.Text.Json.Serialization;
using SunshineGameFinder.Core.Utilities;

namespace SunshineGameFinder.Core.Models;

public class SunshineApp
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    private string _imagePath = string.Empty;
    
    [JsonPropertyName("image-path")]
    public string ImagePath
    {
        get => _imagePath;
        set => _imagePath = PathFormatter.FormatPath(value);
    }

    [JsonPropertyName("output")]
    public string? Output { get; set; }

    private string? _cmd;
    
    [JsonPropertyName("cmd")]
    public string? Cmd
    {
        get => _cmd?.Trim('"');
        set => _cmd = value != null ? PathFormatter.FormatPath(value) : null;
    }

    private string? _workingDir;
    
    [JsonPropertyName("working-dir")]
    public string? WorkingDir
    {
        get => _workingDir;
        set => _workingDir = value != null ? PathFormatter.FormatPath(value) : null;
    }

    [JsonPropertyName("exclude-global-prep-cmd")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? ExcludeGlobalPrepCmd { get; set; }

    [JsonPropertyName("elevated")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Elevated { get; set; }

    [JsonPropertyName("auto-detach")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? AutoDetach { get; set; }

    [JsonPropertyName("wait-all")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? WaitAll { get; set; }

    [JsonPropertyName("exit-timeout")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? ExitTimeout { get; set; }

    [JsonPropertyName("prep-cmd")]
    public List<PrepCmd>? PrepCmd { get; set; }

    private List<string>? _detached;
    
    [JsonPropertyName("detached")]
    public List<string>? Detached
    {
        get => _detached;
        set => _detached = value?.Select(path => PathFormatter.FormatPath(path)).ToList();
    }
}

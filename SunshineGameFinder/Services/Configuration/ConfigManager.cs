using System.Text.Json;
using SunshineGameFinder.Core.Models;
using SunshineGameFinder.Core.Utilities;
using SunshineGameFinder.Infrastructure.FileSystem;
using SunshineGameFinder.Infrastructure.Logging;

namespace SunshineGameFinder.Services.Configuration;

internal sealed class ConfigManager
{
    public SunshineConfig? LoadConfig(string configPath)
    {
        if (!File.Exists(configPath))
        {
            Logger.Log($"Could not find Sunshine Apps config at specified path: {configPath}", LogLevel.Error);
            return null;
        }

        try
        {
            var jsonContent = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<SunshineConfig>(jsonContent, SourceGenerationContext.Default.SunshineConfig);
            
            config ??= new SunshineConfig { Env = new Env() };
            config.Apps ??= new List<SunshineApp>();
            config.Env ??= new Env();
            
            return config;
        }
        catch (Exception ex)
        {
            Logger.Log($"Error loading configuration: {ex.Message}", LogLevel.Error);
            return null;
        }
    }

    public bool SaveConfig(string configPath, SunshineConfig config)
    {
        return ConfigFileWriter.UpdateConfig(configPath, config);
    }
}

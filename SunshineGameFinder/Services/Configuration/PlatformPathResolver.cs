using SunshineGameFinder.Core.Models;
using SunshineGameFinder.Infrastructure.Logging;

namespace SunshineGameFinder.Services.Configuration;

internal sealed class PlatformPathResolver
{
    private const string SunshineDefaultPath = @"C:\Program Files\Sunshine\config\apps.json";
    private const string ApolloDefaultPath = @"C:\Program Files\Apollo\config\apps.json";

    public string ResolveConfigPath(StreamingPlatform platform, string? customPath = null)
    {
        if (!string.IsNullOrEmpty(customPath))
        {
            return customPath;
        }

        return platform switch
        {
            StreamingPlatform.Sunshine => SunshineDefaultPath,
            StreamingPlatform.Apollo => ApolloDefaultPath,
            _ => SunshineDefaultPath
        };
    }

    public StreamingPlatform DetectPlatform(string configPath)
    {
        if (configPath.Contains("Apollo", StringComparison.OrdinalIgnoreCase))
        {
            return StreamingPlatform.Apollo;
        }

        if (configPath.Contains("Sunshine", StringComparison.OrdinalIgnoreCase))
        {
            return StreamingPlatform.Sunshine;
        }

        // Try to detect by checking which config file exists
        if (File.Exists(ApolloDefaultPath))
        {
            Logger.Log($"Detected Apollo platform (config found at {ApolloDefaultPath})", LogLevel.Trace);
            return StreamingPlatform.Apollo;
        }

        if (File.Exists(SunshineDefaultPath))
        {
            Logger.Log($"Detected Sunshine platform (config found at {SunshineDefaultPath})", LogLevel.Trace);
            return StreamingPlatform.Sunshine;
        }

        // Default to Sunshine if neither is found
        Logger.Log("Could not detect platform, defaulting to Sunshine", LogLevel.Warning);
        return StreamingPlatform.Sunshine;
    }

    public string GetServiceName(StreamingPlatform platform)
    {
        return platform switch
        {
            StreamingPlatform.Sunshine => "SunshineService",
            StreamingPlatform.Apollo => "ApolloService",
            _ => "SunshineService"
        };
    }

    public string GetPlatformDisplayName(StreamingPlatform platform)
    {
        return platform switch
        {
            StreamingPlatform.Sunshine => "Sunshine",
            StreamingPlatform.Apollo => "Apollo",
            _ => "Sunshine"
        };
    }
}

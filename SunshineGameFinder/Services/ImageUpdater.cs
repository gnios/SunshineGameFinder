using System.Text.RegularExpressions;
using SunshineGameFinder.Core.Models;
using SunshineGameFinder.Infrastructure.Logging;

namespace SunshineGameFinder.Services;

internal sealed class ImageUpdater
{
    private readonly ImageScraper _imageScraper;
    private readonly string _coversFolderPath;
    private readonly StreamingPlatform _platform;

    public ImageUpdater(ImageScraper imageScraper, string coversFolderPath, StreamingPlatform platform)
    {
        _imageScraper = imageScraper;
        _coversFolderPath = coversFolderPath;
        _platform = platform;
    }

    public async Task<int> UpdateMissingImagesAsync(SunshineConfig config)
    {
        if (config.Apps == null)
            return 0;

        var updatedCount = 0;
        Logger.Log("Checking for games with missing images...");

        foreach (var app in config.Apps)
        {
            // Skip special apps (Desktop, Steam Big Picture)
            if (app.Name == "Desktop" || app.Name == "Steam Big Picture")
            {
                continue;
            }

            // Skip if image already exists and file is valid
            if (!string.IsNullOrEmpty(app.ImagePath))
            {
                string fullImagePath;
                if (Path.IsPathRooted(app.ImagePath))
                {
                    fullImagePath = app.ImagePath;
                }
                else
                {
                    fullImagePath = Path.Combine(_coversFolderPath, app.ImagePath);
                }
                
                if (File.Exists(fullImagePath))
                {
                    Logger.Log($"Image already exists for: {app.Name} ({app.ImagePath})", LogLevel.Trace);
                    continue;
                }
            }

            Logger.Log($"Updating image for: {app.Name}");

            // Try to extract Steam App ID from command
            int? steamAppId = null;
            if (!string.IsNullOrEmpty(app.Cmd) && app.Cmd.StartsWith("steam://rungameid/", StringComparison.OrdinalIgnoreCase))
            {
                steamAppId = ExtractSteamAppIdFromCommand(app.Cmd);
                if (steamAppId.HasValue)
                {
                    Logger.Log($"\tExtracted Steam App ID: {steamAppId.Value}", LogLevel.Trace);
                }
            }

            // Try to update image (will search by ID first, then by name)
            var imagePath = await _imageScraper.SaveIGDBImageToCoversFolder(app.Name, _coversFolderPath, steamAppId);
            
            if (!string.IsNullOrEmpty(imagePath))
            {
                // Apollo requires absolute paths, Sunshine can use relative paths
                if (_platform == StreamingPlatform.Apollo)
                {
                    app.ImagePath = imagePath;
                }
                else
                {
                    // Store relative path if in covers folder, absolute otherwise
                    var relativePath = Path.GetRelativePath(_coversFolderPath, imagePath);
                    if (!relativePath.StartsWith(".."))
                    {
                        app.ImagePath = relativePath;
                    }
                    else
                    {
                        app.ImagePath = imagePath;
                    }
                }
                updatedCount++;
                Logger.Log($"\t✓ Updated image for: {app.Name} -> {app.ImagePath}", LogLevel.Success);
            }
            else
            {
                Logger.Log($"\t✗ Could not find image for: {app.Name}", LogLevel.Warning);
            }
        }

        return updatedCount;
    }

    private static int? ExtractSteamAppIdFromCommand(string command)
    {
        try
        {
            // Match pattern: steam://rungameid/{ID}
            var match = Regex.Match(command, @"steam://rungameid/(\d+)", RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                if (int.TryParse(match.Groups[1].Value, out var appId))
                {
                    return appId;
                }
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return null;
    }
}

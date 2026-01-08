using SunshineGameFinder.Core.Models;
using SunshineGameFinder.Infrastructure.Logging;
using SunshineGameFinder.Services.CommandLine;
using SunshineGameFinder.Services.Configuration;
using SunshineGameFinder.Services.GameDiscovery;

namespace SunshineGameFinder.Services;

internal sealed class ApplicationOrchestrator
{
    private readonly ConfigManager _configManager;
    private readonly GameDirectoryInitializer _directoryInitializer;
    private readonly SpecialAppEnsurer _specialAppEnsurer;
    private readonly UninstalledGameRemover _uninstalledGameRemover;
    private readonly PlatformPathResolver _pathResolver;

    public ApplicationOrchestrator(
        ConfigManager configManager,
        GameDirectoryInitializer directoryInitializer,
        SpecialAppEnsurer specialAppEnsurer,
        UninstalledGameRemover uninstalledGameRemover)
    {
        _configManager = configManager;
        _directoryInitializer = directoryInitializer;
        _specialAppEnsurer = specialAppEnsurer;
        _uninstalledGameRemover = uninstalledGameRemover;
        _pathResolver = new PlatformPathResolver();
    }

    public async Task<int> RunAsync(ApplicationOptions options)
    {
        // Determine platform and resolve config path
        StreamingPlatform platform;
        string configPath;

        if (options.TargetPlatform.HasValue)
        {
            platform = options.TargetPlatform.Value;
            configPath = _pathResolver.ResolveConfigPath(platform, options.SunshineConfigLocation);
        }
        else if (!string.IsNullOrEmpty(options.SunshineConfigLocation))
        {
            configPath = options.SunshineConfigLocation;
            platform = _pathResolver.DetectPlatform(configPath);
        }
        else
        {
            // Auto-detect platform
            platform = _pathResolver.DetectPlatform(string.Empty);
            configPath = _pathResolver.ResolveConfigPath(platform);
        }

        var platformName = _pathResolver.GetPlatformDisplayName(platform);
        Logger.Log($"Using {platformName} platform. Config path: {configPath}");

        var config = _configManager.LoadConfig(configPath);
        if (config == null)
        {
            Logger.Log($"Could not load {platformName} config from: {configPath}", LogLevel.Error);
            return 1;
        }

        var gamesRemoved = 0;
        if (options.RemoveUninstalled)
        {
            gamesRemoved = _uninstalledGameRemover.RemoveUninstalledGames(config);
        }

        var gameDirs = _directoryInitializer.InitializeDirectories(options.AdditionalDirectories);
        var rootFolder = Path.GetDirectoryName(configPath) ?? string.Empty;
        var coversFolderPath = Path.Combine(rootFolder, "covers");

        using var httpClient = new HttpClient();
        // SteamGridDB API key can be set via environment variable STEAMGRIDDB_API_KEY
        var steamGridDbApiKey = Environment.GetEnvironmentVariable("STEAMGRIDDB_API_KEY");
        var imageScraper = new ImageScraper(httpClient, steamGridDbApiKey);

        // Update missing images if requested
        if (options.UpdateImages)
        {
            var imageUpdater = new ImageUpdater(imageScraper, coversFolderPath, platform);
            var imagesUpdated = await imageUpdater.UpdateMissingImagesAsync(config);
            
            if (imagesUpdated > 0)
            {
                if (_configManager.SaveConfig(configPath, config))
                {
                    Logger.Log($"Updated {imagesUpdated} game images in {platformName} config.", LogLevel.Success);
                }
            }
            else
            {
                Logger.Log("No images needed updating.", LogLevel.Success);
            }
            
            if (!options.NoWait)
            {
                PromptForExit();
            }
            return 0;
        }

        var exclusionWords = new List<string> { "Steam" };
        var exeExclusionWords = new List<string> 
        { 
            "Steam", "Cleanup", "DX", "Uninstall", "Touchup", 
            "redist", "Crash", "Editor", "crs-handler" 
        };
        exeExclusionWords.AddRange(options.AdditionalExeExclusionWords);

        var gameScanner = new GameScanner(exclusionWords, exeExclusionWords);
        var discoveryService = new GameDiscoveryService(gameScanner, imageScraper, coversFolderPath, platform);

        var gamesAdded = await discoveryService.DiscoverGamesAsync(
            gameDirs, 
            config, 
            options.ForceUpdate);

        _specialAppEnsurer.EnsureSpecialApps(
            config, 
            options.EnsureDesktop, 
            options.EnsureSteamBigPicture);

        Logger.Log("Game Discovery Completed");

        if (gamesAdded > 0 || gamesRemoved > 0 || options.ForceUpdate)
        {
            if (_configManager.SaveConfig(configPath, config))
            {
                Logger.Log(
                    $"{platformName} apps config updated! {gamesAdded} apps were added. {gamesRemoved} apps were removed. " +
                    $"Check {platformName} to ensure all games were added.", 
                    LogLevel.Success);
            }
        }
        else
        {
            Logger.Log($"No new games were found to be added to {platformName}");
        }

        if (!options.NoWait)
        {
            await PromptForServiceRestartAsync(platform);
            PromptForExit();
        }

        return 0;
    }

    private async Task PromptForServiceRestartAsync(StreamingPlatform platform)
    {
        var platformName = _pathResolver.GetPlatformDisplayName(platform);
        Console.Write($"\nWould you like to restart the {platformName} service now? (Y/N): ");
        var restartResponse = Console.ReadLine();
        
        if (!string.IsNullOrEmpty(restartResponse) && 
            restartResponse.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase))
        {
            Logger.Log($"Attempting to restart {platformName} service...", LogLevel.Trace);
            await StreamingServiceManager.RestartServiceAsync(platform);
        }
    }

    private static void PromptForExit()
    {
        Logger.Log("\nPress any key to exit...");
        Console.ReadKey();
    }
}

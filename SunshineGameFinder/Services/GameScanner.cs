using SunshineGameFinder.Core.Models;
using SunshineGameFinder.Infrastructure.Logging;

namespace SunshineGameFinder.Services;

internal class GameScanner
{
    private readonly HashSet<string> _exclusionWords;
    private readonly HashSet<string> _exeExclusionWords;
    private readonly HashSet<string> _scannedFolders = new();
    private readonly SteamAppIdExtractor _steamAppIdExtractor;

    public GameScanner(IEnumerable<string> exclusionWords, IEnumerable<string> exeExclusionWords)
    {
        _exclusionWords = new HashSet<string>(exclusionWords, StringComparer.OrdinalIgnoreCase);
        _exeExclusionWords = new HashSet<string>(exeExclusionWords, StringComparer.OrdinalIgnoreCase);
        _steamAppIdExtractor = new SteamAppIdExtractor();
    }

    public async Task<int> ScanFolderAsync(
        string folder, 
        SunshineConfig config, 
        bool forceUpdate,
        ImageScraper imageScraper,
        string coversFolderPath,
        StreamingPlatform platform)
    {
        if (string.IsNullOrEmpty(folder) || _scannedFolders.Contains(folder))
            return 0;

        _scannedFolders.Add(folder);
        Logger.Log($"Scanning for games in {folder}...");

        var directoryInfo = new DirectoryInfo(folder);
        if (!directoryInfo.Exists)
        {
            Logger.Log($"Directory {directoryInfo.Name} does not exist, skipping...", LogLevel.Warning);
            return 0;
        }

        config.Apps ??= new List<SunshineApp>();

        var gamesAdded = 0;
        foreach (var gameDir in directoryInfo.GetDirectories())
        {
            try
            {
                if (await ProcessGameDirectoryAsync(gameDir, folder, config, forceUpdate, imageScraper, coversFolderPath, platform))
                {
                    gamesAdded++;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error processing {gameDir.Name}: {ex.Message}", LogLevel.Error);
            }
        }

        Console.WriteLine();
        return gamesAdded;
    }

    private async Task<bool> ProcessGameDirectoryAsync(
        DirectoryInfo gameDir,
        string baseFolder,
        SunshineConfig config,
        bool forceUpdate,
        ImageScraper imageScraper,
        string coversFolderPath,
        StreamingPlatform platform)
    {
        Logger.Log($"\tLooking in {gameDir.FullName.Replace(baseFolder, "")}...", false);

        var gameName = CleanGameName(gameDir.Name);
        
        if (_exclusionWords.Any(ew => gameName.Contains(ew, StringComparison.OrdinalIgnoreCase)))
        {
            Logger.Log("Skipping due to excluded word match", LogLevel.Trace);
            return false;
        }

        // First, try to extract Steam App ID to check if this is a Steam game
        var steamAppId = _steamAppIdExtractor.ExtractSteamAppId(gameDir.FullName);
        string? command = null;
        bool isSteamGame = false;

        if (steamAppId.HasValue)
        {
            // This is a Steam game - use steam://rungameid/{GAMEID}
            command = $"steam://rungameid/{steamAppId.Value}";
            isSteamGame = true;
            Logger.Log($"Detected Steam game (App ID: {steamAppId.Value})", LogLevel.Trace);
        }
        else
        {
            // Not a Steam game - find the executable
            var exe = FindGameExecutable(gameDir);
            if (string.IsNullOrEmpty(exe))
            {
                Logger.Log("EXE not found", LogLevel.Warning);
                return false;
            }
            command = exe;
        }

        var existingApp = config.Apps.FirstOrDefault(g => 
            g.Cmd == command || g.Name == gameName);

        if (forceUpdate && existingApp != null)
        {
            config.Apps.Remove(existingApp);
            existingApp = null;
        }

        if (existingApp == null)
        {
            existingApp = CreateSunshineApp(gameName, command, isSteamGame);
            
            // Use Steam App ID for image scraping (if available)
            var coverImagePath = await imageScraper.SaveIGDBImageToCoversFolder(gameName, coversFolderPath, steamAppId);
            if (!string.IsNullOrEmpty(coverImagePath))
            {
                // Apollo requires absolute paths, Sunshine can use relative paths
                if (platform == StreamingPlatform.Apollo)
                {
                    existingApp.ImagePath = coverImagePath;
                }
                else
                {
                    // Store relative path if in covers folder, absolute otherwise
                    var relativePath = Path.GetRelativePath(coversFolderPath, coverImagePath);
                    if (!relativePath.StartsWith(".."))
                    {
                        existingApp.ImagePath = relativePath;
                    }
                    else
                    {
                        existingApp.ImagePath = coverImagePath;
                    }
                }
            }
            else
            {
                Logger.Log($"Failed to find cover image for {gameName}", LogLevel.Warning);
            }

            config.Apps.Add(existingApp);
            var commandDisplay = isSteamGame ? $"steam://rungameid/{steamAppId}" : command;
            Logger.Log($"Adding new game to Sunshine apps: {gameName} - {commandDisplay}", LogLevel.Success);
            return true;
        }
        else
        {
            var appInfo = existingApp.Cmd ?? existingApp.Detached?.FirstOrDefault() ?? existingApp.Name;
            Logger.Log($"Found existing Sunshine app for {gameName} already!: {appInfo.Trim()}");
            return false;
        }
    }

    private string? FindGameExecutable(DirectoryInfo gameDir)
    {
        var exeFiles = Directory.GetFiles(gameDir.FullName, "*.exe", SearchOption.AllDirectories);
        
        return exeFiles.FirstOrDefault(exeFile =>
        {
            var exeName = Path.GetFileName(exeFile).ToLower();
            var gameNameLower = gameDir.Name.ToLower();
            var cleanGameNameLower = CleanGameName(gameDir.Name).ToLower();

            return exeName == gameNameLower || 
                   exeName == cleanGameNameLower || 
                   !_exeExclusionWords.Any(ew => exeName.Contains(ew.ToLower()));
        });
    }

    private static SunshineApp CreateSunshineApp(string gameName, string command, bool isSteamGame = false)
    {
        // For Steam games, use the steam:// protocol command
        if (isSteamGame)
        {
            return new SunshineApp
            {
                Name = gameName,
                Cmd = command,
                WorkingDir = string.Empty
            };
        }

        // For non-Steam games, check if it's a gamelaunchhelper
        if (command.Contains("gamelaunchhelper.exe", StringComparison.OrdinalIgnoreCase))
        {
            return new SunshineApp
            {
                Name = gameName,
                Detached = new List<string> { command },
                WorkingDir = string.Empty
            };
        }

        // Regular executable
        return new SunshineApp
        {
            Name = gameName,
            Cmd = command,
            WorkingDir = string.Empty
        };
    }

    private static string CleanGameName(string name)
    {
        var toReplace = new[] { "Win10", "Windows 10", "Win11", "Windows 11" };
        return toReplace.Aggregate(name, (current, remove) => current.Replace(remove, "")).Trim();
    }
}

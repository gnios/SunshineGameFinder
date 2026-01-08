using System.Text.RegularExpressions;
using SunshineGameFinder.Core.Models;
using SunshineGameFinder.Infrastructure.Logging;

namespace SunshineGameFinder.Services.GameDiscovery;

internal sealed class GameDiscoveryService
{
    private readonly GameScanner _gameScanner;
    private readonly ImageScraper _imageScraper;
    private readonly string _coversFolderPath;
    private readonly StreamingPlatform _platform;

    public GameDiscoveryService(
        GameScanner gameScanner,
        ImageScraper imageScraper,
        string coversFolderPath,
        StreamingPlatform platform)
    {
        _gameScanner = gameScanner;
        _imageScraper = imageScraper;
        _coversFolderPath = coversFolderPath;
        _platform = platform;
    }

    public async Task<int> DiscoverGamesAsync(
        HashSet<string> gameDirs,
        SunshineConfig config,
        bool forceUpdate)
    {
        const string wildcardDrive = @"*:\";
        var wildcardDriveRegex = new Regex(Regex.Escape(wildcardDrive));
        var logicalDrives = DriveInfo.GetDrives();
        var totalGamesAdded = 0;

        foreach (var platformDir in gameDirs)
        {
            if (platformDir.StartsWith(wildcardDrive, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var drive in logicalDrives)
                {
                    var resolvedPath = wildcardDriveRegex.Replace(platformDir, drive.Name, 1);
                    totalGamesAdded += await _gameScanner.ScanFolderAsync(
                        resolvedPath, 
                        config, 
                        forceUpdate, 
                        _imageScraper, 
                        _coversFolderPath,
                        _platform);
                }
            }
            else
            {
                totalGamesAdded += await _gameScanner.ScanFolderAsync(
                    platformDir, 
                    config, 
                    forceUpdate, 
                    _imageScraper, 
                    _coversFolderPath,
                    _platform);
            }
        }

        return totalGamesAdded;
    }
}

using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using SunshineGameFinder.Infrastructure.Logging;

namespace SunshineGameFinder.Services;

internal class SteamLibraryFinder
{
    private const string SteamLibraryFolders = @"Program Files (x86)\Steam\steamapps\libraryfolders.vdf";

    public HashSet<string> FindSteamLibraries()
    {
        var gameDirs = new HashSet<string>();
        var logicalDrives = DriveInfo.GetDrives();

        foreach (var drive in logicalDrives)
        {
            var libraryFoldersPath = Path.Combine(drive.Name, SteamLibraryFolders);
            var file = new FileInfo(libraryFoldersPath);
            
            if (!file.Exists)
            {
                Logger.Log($"libraryfolders.vdf not found on {file.DirectoryName}, skipping...", LogLevel.Warning);
                continue;
            }

            try
            {
                var libraries = VdfConvert.Deserialize(File.ReadAllText(libraryFoldersPath));
                
                foreach (var library in libraries.Value)
                {
                    if (library is not VProperty libProp)
                        continue;

                    try
                    {
                        var libraryPath = libProp.Value.Value<string>("path");
                        if (!string.IsNullOrEmpty(libraryPath))
                        {
                            var steamAppsPath = Path.Combine(libraryPath, "steamapps", "common");
                            gameDirs.Add(steamAppsPath);
                            Logger.Log($"Found VDF library: {libraryPath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Failed to parse VDF library value: '{ex.Message}' at {libraryFoldersPath}", LogLevel.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to parse libraryfolders.vdf: '{ex.Message}' at {libraryFoldersPath}", LogLevel.Warning);
            }
        }

        return gameDirs;
    }
}

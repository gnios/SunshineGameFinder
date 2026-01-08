using System.Text.RegularExpressions;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;

namespace SunshineGameFinder.Services;

internal sealed class SteamAppIdExtractor
{
    public int? ExtractSteamAppId(string gameDirectoryPath)
    {
        try
        {
            var directoryInfo = new DirectoryInfo(gameDirectoryPath);
            if (!directoryInfo.Exists)
                return null;

            // For Steam games, the appmanifest is usually in the parent steamapps folder
            // Check if we're in a steamapps/common directory structure
            var currentDir = directoryInfo;
            DirectoryInfo? steamAppsFolder = null;
            
            // Walk up the directory tree to find steamapps folder
            while (currentDir != null)
            {
                if (currentDir.Name.Equals("steamapps", StringComparison.OrdinalIgnoreCase))
                {
                    steamAppsFolder = currentDir;
                    break;
                }
                currentDir = currentDir.Parent;
            }

            if (steamAppsFolder == null)
                return null;

            // Look for appmanifest files
            var manifestFiles = steamAppsFolder.GetFiles("appmanifest_*.acf");
            
            foreach (var manifestFile in manifestFiles)
            {
                try
                {
                    var content = File.ReadAllText(manifestFile.FullName);
                    var vdf = VdfConvert.Deserialize(content);
                    
                    // Check if this manifest is for the game in this directory
                    var installDir = vdf.Value.Value<string>("installdir");
                    if (string.IsNullOrEmpty(installDir))
                        continue;

                    // Compare the install directory name with the game directory name
                    if (installDir.Equals(directoryInfo.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        // First try to get appid from the manifest content
                        var appId = vdf.Value.Value<string>("appid");
                        if (!string.IsNullOrEmpty(appId) && int.TryParse(appId, out var appIdInt))
                        {
                            return appIdInt;
                        }
                        
                        // Fallback: extract from filename: appmanifest_{appid}.acf
                        var fileName = Path.GetFileNameWithoutExtension(manifestFile.Name);
                        if (fileName.StartsWith("appmanifest_", StringComparison.OrdinalIgnoreCase))
                        {
                            var appIdStr = fileName.Substring("appmanifest_".Length);
                            if (int.TryParse(appIdStr, out var appIdFromFile))
                            {
                                return appIdFromFile;
                            }
                        }
                    }
                }
                catch
                {
                    // Continue to next manifest file
                    continue;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}

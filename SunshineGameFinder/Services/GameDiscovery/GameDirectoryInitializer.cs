namespace SunshineGameFinder.Services.GameDiscovery;

internal sealed class GameDirectoryInitializer
{
    private readonly SteamLibraryFinder _steamLibraryFinder;

    public GameDirectoryInitializer(SteamLibraryFinder steamLibraryFinder)
    {
        _steamLibraryFinder = steamLibraryFinder;
    }

    public HashSet<string> InitializeDirectories(string[] additionalDirectories)
    {
        var gameDirs = new HashSet<string>
        {
            @"*:\Program Files (x86)\Steam\steamapps\common",
            @"*:\XboxGames",
            @"*:\Program Files\EA Games",
            @"*:\Program Files\Epic Games\",
            @"*:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher\games"
        };

        foreach (var dir in additionalDirectories)
        {
            if (Directory.Exists(dir))
            {
                gameDirs.Add(dir);
            }
        }

        var steamLibraries = _steamLibraryFinder.FindSteamLibraries();
        gameDirs.UnionWith(steamLibraries);

        return gameDirs;
    }
}

using SunshineGameFinder.Core.Models;
using SunshineGameFinder.Infrastructure.Logging;

namespace SunshineGameFinder.Services;

internal class UninstalledGameRemover
{
    public int RemoveUninstalledGames(SunshineConfig config)
    {
        if (config.Apps == null)
            return 0;

        var gamesRemoved = 0;

        for (int i = config.Apps.Count - 1; i >= 0; i--)
        {
            var existingApp = config.Apps[i];
            if (existingApp == null)
                continue;

            if (!IsExecutableStillExists(existingApp))
            {
                Logger.Log($"{existingApp.Name} no longer has an exe, removing from apps config...", LogLevel.Error);
                config.Apps.RemoveAt(i);
                gamesRemoved++;
            }
        }

        return gamesRemoved;
    }

    private static bool IsExecutableStillExists(SunshineApp app)
    {
        if (app.Cmd == null && app.Detached == null)
            return true;

        if (app.Cmd?.StartsWith("steam://", StringComparison.OrdinalIgnoreCase) == true)
            return true;

        if (app.Cmd != null && File.Exists(app.Cmd))
            return true;

        if (app.Detached != null)
        {
            return app.Detached.All(detachedCommand =>
            {
                if (string.IsNullOrEmpty(detachedCommand))
                    return true;

                if (!detachedCommand.Contains("exe", StringComparison.OrdinalIgnoreCase))
                    return true;

                return detachedCommand.EndsWith("exe", StringComparison.OrdinalIgnoreCase) && 
                       File.Exists(detachedCommand);
            });
        }

        return false;
    }
}

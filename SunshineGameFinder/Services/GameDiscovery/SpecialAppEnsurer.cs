using SunshineGameFinder.Core.Models;

namespace SunshineGameFinder.Services.GameDiscovery;

internal sealed class SpecialAppEnsurer
{
    public void EnsureSpecialApps(SunshineConfig config, bool ensureDesktop, bool ensureSteamBigPicture)
    {
        config.Apps ??= new List<SunshineApp>();

        if (ensureDesktop && !config.Apps.Any(app => app.Name == "Desktop"))
        {
            config.Apps.Add(new DesktopApp());
        }

        if (ensureSteamBigPicture && 
            !config.Apps.Any(app => app.Name == "Steam Big Picture" || app.Cmd == "steam://open/bigpicture"))
        {
            config.Apps.Add(new SteamBigPictureApp());
        }
    }
}

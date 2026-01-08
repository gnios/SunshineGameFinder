namespace SunshineGameFinder.Core.Models;

public class SteamBigPictureApp : SunshineApp
{
    public SteamBigPictureApp()
    {
        Name = "Steam Big Picture";
        Cmd = "steam://open/bigpicture";
        AutoDetach = "true";
        WaitAll = "true";
        ImagePath = "steam.png";
    }
}

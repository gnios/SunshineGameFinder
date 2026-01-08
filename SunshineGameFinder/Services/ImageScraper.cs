using SunshineGameFinder.Services.SteamGridDb;

namespace SunshineGameFinder.Services;

internal sealed class ImageScraper
{
    private readonly SteamGridDbService _steamGridDbService;

    public ImageScraper(HttpClient httpClient, string? steamGridDbApiKey = null)
    {
        _steamGridDbService = new SteamGridDbService(httpClient, steamGridDbApiKey);
    }

    public async Task<string?> SaveIGDBImageToCoversFolder(string gameName, string coversFolderPath, int? steamAppId = null)
    {
        return await _steamGridDbService.DownloadGameImageAsync(gameName, coversFolderPath, steamAppId);
    }
}

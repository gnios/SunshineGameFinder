using System.Text.Json;
using System.Text.Json.Serialization;
using SunshineGameFinder.Infrastructure.Logging;

namespace SunshineGameFinder.Services.SteamGridDb;

internal sealed class SteamGridDbService
{
    private const string BaseUrl = "https://www.steamgriddb.com/api/v2";
    private const string SearchEndpoint = "/search/autocomplete/{0}";
    private const string GridsEndpoint = "/grids/game/{0}";
    
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;

    public SteamGridDbService(HttpClient httpClient, string? apiKey = null)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }
    }

    public async Task<string?> DownloadGameImageAsync(string gameName, string coversFolderPath, int? steamAppId = null)
    {
        try
        {
            Logger.Log($"\t\tSearching for cover image: {gameName}" + (steamAppId.HasValue ? $" (Steam App ID: {steamAppId.Value})" : ""), LogLevel.Trace);
            
            if (!Directory.Exists(coversFolderPath))
            {
                Directory.CreateDirectory(coversFolderPath);
            }

            string? gridUrl = null;
            int? gameId = null;

            // If we have Steam App ID, get grids directly from /grids/steam/{steamAppId}
            if (steamAppId.HasValue)
            {
                Logger.Log($"\t\t\tAttempting to get grids by Steam App ID: {steamAppId.Value}", LogLevel.Trace);
                gridUrl = await GetBestGridImageUrlBySteamAppIdAsync(steamAppId.Value);
                if (!string.IsNullOrEmpty(gridUrl))
                {
                    Logger.Log($"\t\t\tFound grid image by Steam App ID", LogLevel.Trace);
                }
                else
                {
                    Logger.Log($"\t\t\tNo grid image found by Steam App ID: {steamAppId.Value}, trying by name...", LogLevel.Trace);
                }
            }

            // If not found by Steam App ID, search by name with multiple variations
            if (string.IsNullOrEmpty(gridUrl))
            {
                var nameVariations = GenerateNameVariations(gameName);
                Logger.Log($"\t\t\tSearching by name with {nameVariations.Count} variations", LogLevel.Trace);
                
                foreach (var nameVariation in nameVariations)
                {
                    Logger.Log($"\t\t\tTrying name variation: '{nameVariation}'", LogLevel.Trace);
                    gameId = await FindGameByNameAsync(nameVariation);
                    if (gameId.HasValue)
                    {
                        Logger.Log($"\t\t\tFound game by name '{nameVariation}': Game ID = {gameId.Value}", LogLevel.Trace);
                        break;
                    }
                }

                if (gameId.HasValue)
                {
                    // Get grids for the game
                    Logger.Log($"\t\t\tFetching grid images for Game ID: {gameId.Value}", LogLevel.Trace);
                    gridUrl = await GetBestGridImageUrlAsync(gameId.Value);
                }
            }

            if (string.IsNullOrEmpty(gridUrl))
            {
                Logger.Log($"\t\tCould not find grid image in SteamGridDB for: {gameName}", LogLevel.Warning);
                return null;
            }

            Logger.Log($"\t\t\tDownloading image from: {gridUrl}", LogLevel.Trace);
            // Download and save the image
            var imageStream = await _httpClient.GetStreamAsync(gridUrl);
            var fileName = steamAppId.HasValue 
                ? $"{steamAppId.Value}.png" 
                : gameId.HasValue 
                    ? $"{gameId.Value}.png" 
                    : $"{Path.GetFileNameWithoutExtension(gridUrl)}.png";
            var fullPath = Path.Combine(coversFolderPath, fileName);

            await using var fileStream = new FileStream(fullPath, FileMode.OpenOrCreate);
            await imageStream.CopyToAsync(fileStream);

            Logger.Log($"\t\t\tImage saved to: {fullPath}", LogLevel.Trace);
            return fullPath;
        }
        catch (Exception ex)
        {
            Logger.Log($"\t\tError downloading image from SteamGridDB for {gameName}: {ex.Message}", LogLevel.Error);
            Logger.Log($"\t\t\tStack trace: {ex.StackTrace}", LogLevel.Trace);
            return null;
        }
    }

    private static List<string> GenerateNameVariations(string gameName)
    {
        var variations = new List<string> { gameName };
        
        // Original name
        if (!variations.Contains(gameName))
            variations.Add(gameName);
        
        // Replace underscores with spaces
        var withSpaces = gameName.Replace("_", " ");
        if (!variations.Contains(withSpaces))
            variations.Add(withSpaces);
        
        // Replace underscores with nothing
        var withoutUnderscores = gameName.Replace("_", "");
        if (!variations.Contains(withoutUnderscores))
            variations.Add(withoutUnderscores);
        
        // Replace multiple spaces with single space
        var normalized = System.Text.RegularExpressions.Regex.Replace(withSpaces, @"\s+", " ").Trim();
        if (!variations.Contains(normalized))
            variations.Add(normalized);
        
        // Remove common suffixes/prefixes
        var withoutOnline = normalized.Replace(" ONLINE", "").Replace(" Online", "").Trim();
        if (!variations.Contains(withoutOnline) && withoutOnline != normalized)
            variations.Add(withoutOnline);
        
        return variations;
    }

    private async Task<string?> GetBestGridImageUrlBySteamAppIdAsync(int steamAppId)
    {
        try
        {
            // Get grids directly by Steam App ID
            var url = $"{BaseUrl}/grids/steam/{steamAppId}";
            Logger.Log($"\t\t\t\tGET {url}", LogLevel.Trace);
            var response = await _httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();
            
            Logger.Log($"\t\t\t\tResponse status: {response.StatusCode}", LogLevel.Trace);
            Logger.Log($"\t\t\t\tResponse body (first 500 chars): {(json.Length > 500 ? json.Substring(0, 500) + "..." : json)}", LogLevel.Trace);
            
            if (!response.IsSuccessStatusCode)
            {
                Logger.Log($"\t\t\t\tFailed to get grids by Steam App ID: {response.StatusCode}", LogLevel.Trace);
                return null;
            }

            var result = JsonSerializer.Deserialize<SteamGridDbResponse<GridData[]>>(json);

            if (result?.Data == null || result.Data.Length == 0)
            {
                Logger.Log($"\t\t\t\tNo grids found for Steam App ID: {steamAppId}", LogLevel.Trace);
                return null;
            }

            Logger.Log($"\t\t\t\tFound {result.Data.Length} grids", LogLevel.Trace);
            
            // Prefer verified grids, then highest score, then first available
            var bestGrid = result.Data
                .OrderByDescending(g => g.Verified ? 1 : 0)
                .ThenByDescending(g => g.Score ?? 0)
                .FirstOrDefault();

            if (bestGrid != null)
            {
                Logger.Log($"\t\t\t\tSelected grid: ID={bestGrid.Id}, Verified={bestGrid.Verified}, Score={bestGrid.Score}, URL={bestGrid.Url}", LogLevel.Trace);
                return bestGrid.Url;
            }

            Logger.Log($"\t\t\t\tNo suitable grid found", LogLevel.Trace);
            return null;
        }
        catch (Exception ex)
        {
            Logger.Log($"\t\t\t\tException in GetBestGridImageUrlBySteamAppIdAsync: {ex.Message}", LogLevel.Error);
            Logger.Log($"\t\t\t\tStack trace: {ex.StackTrace}", LogLevel.Trace);
            return null;
        }
    }

    private async Task<int?> FindGameByNameAsync(string gameName)
    {
        try
        {
            var encodedName = Uri.EscapeDataString(gameName);
            var url = $"{BaseUrl}{string.Format(SearchEndpoint, encodedName)}";
            Logger.Log($"\t\t\t\tGET {url}", LogLevel.Trace);
            
            var response = await _httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();
            
            Logger.Log($"\t\t\t\tResponse status: {response.StatusCode}", LogLevel.Trace);
            Logger.Log($"\t\t\t\tResponse body: {json}", LogLevel.Trace);

            if (!response.IsSuccessStatusCode)
            {
                Logger.Log($"\t\t\t\tSearch failed with status: {response.StatusCode}", LogLevel.Trace);
                return null;
            }

            var result = JsonSerializer.Deserialize<SteamGridDbResponse<SearchResult[]>>(json);

            if (result?.Data != null && result.Data.Length > 0)
            {
                Logger.Log($"\t\t\t\tFound {result.Data.Length} search results", LogLevel.Trace);
                for (int i = 0; i < Math.Min(result.Data.Length, 5); i++)
                {
                    Logger.Log($"\t\t\t\t  [{i}] ID={result.Data[i].Id}, Name='{result.Data[i].Name}'", LogLevel.Trace);
                }
                // Return the first match (most relevant)
                return result.Data[0].Id;
            }

            Logger.Log($"\t\t\t\tNo search results found for: {gameName}", LogLevel.Trace);
            return null;
        }
        catch (Exception ex)
        {
            Logger.Log($"\t\t\t\tException in FindGameByNameAsync: {ex.Message}", LogLevel.Error);
            return null;
        }
    }

    private async Task<string?> GetBestGridImageUrlAsync(int gameId)
    {
        try
        {
            var url = $"{BaseUrl}{string.Format(GridsEndpoint, gameId)}";
            Logger.Log($"\t\t\t\tGET {url}", LogLevel.Trace);
            
            var response = await _httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();
            
            Logger.Log($"\t\t\t\tResponse status: {response.StatusCode}", LogLevel.Trace);
            Logger.Log($"\t\t\t\tResponse body: {json}", LogLevel.Trace);

            if (!response.IsSuccessStatusCode)
            {
                Logger.Log($"\t\t\t\tFailed to get grids: {response.StatusCode}", LogLevel.Trace);
                return null;
            }

            var result = JsonSerializer.Deserialize<SteamGridDbResponse<GridData[]>>(json);

            if (result?.Data == null || result.Data.Length == 0)
            {
                Logger.Log($"\t\t\t\tNo grids found for Game ID: {gameId}", LogLevel.Trace);
                return null;
            }

            Logger.Log($"\t\t\t\tFound {result.Data.Length} grids", LogLevel.Trace);
            
            // Prefer verified grids, then highest score, then first available
            var bestGrid = result.Data
                .OrderByDescending(g => g.Verified ? 1 : 0)
                .ThenByDescending(g => g.Score ?? 0)
                .FirstOrDefault();

            if (bestGrid != null)
            {
                Logger.Log($"\t\t\t\tSelected grid: ID={bestGrid.Id}, Verified={bestGrid.Verified}, Score={bestGrid.Score}, URL={bestGrid.Url}", LogLevel.Trace);
                return bestGrid.Url;
            }

            Logger.Log($"\t\t\t\tNo suitable grid found", LogLevel.Trace);
            return null;
        }
        catch (Exception ex)
        {
            Logger.Log($"\t\t\t\tException in GetBestGridImageUrlAsync: {ex.Message}", LogLevel.Error);
            return null;
        }
    }

    private class SteamGridDbResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }

    private class SearchResult
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("types")]
        public string[]? Types { get; set; }
    }

    private class GridData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("gameId")]
        public int GameId { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("verified")]
        public bool Verified { get; set; }

        [JsonPropertyName("score")]
        public int? Score { get; set; }
    }

    private class GameInfo
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}

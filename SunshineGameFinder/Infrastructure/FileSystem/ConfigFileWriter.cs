using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using SunshineGameFinder.Core.Models;
using SunshineGameFinder.Infrastructure.Logging;

namespace SunshineGameFinder.Infrastructure.FileSystem;

internal class ConfigFileWriter
{
    private const string BackupFileExtension = "bak";
    private const int BackupsToKeep = 5;

    public static bool UpdateConfig(string filePath, SunshineConfig config)
    {
        var folderPath = Path.GetDirectoryName(filePath);
        
        try
        {
            var backupFilePath = Path.Combine(
                folderPath ?? string.Empty, 
                $"{Path.GetFileNameWithoutExtension(filePath)}_{DateTime.Now:MMddyyyy_HHmmss}.{BackupFileExtension}");
            
            File.Move(filePath, backupFilePath);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var serializedJson = JsonSerializer.Serialize(config, options);
            File.WriteAllText(filePath, serializedJson);
            
            CleanUpBackups(folderPath ?? string.Empty);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log($"An error occurred while trying to update the configuration file at {filePath}. Exception: {ex.Message}", LogLevel.Error);
            return false;
        }
    }

    private static void CleanUpBackups(string folderPath)
    {
        try
        {
            var directoryInfo = new DirectoryInfo(folderPath);
            var backupFiles = directoryInfo
                .GetFiles()
                .Where(f => f.Extension == $".{BackupFileExtension}")
                .OrderByDescending(f => f.CreationTime)
                .ToArray();

            for (int i = BackupsToKeep; i < backupFiles.Length; i++)
            {
                backupFiles[i].Delete();
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"An error occurred while trying to clean up historic backup files. This should not impact the validity of the new configuration. Exception: {ex.Message}", LogLevel.Warning);
        }
    }
}

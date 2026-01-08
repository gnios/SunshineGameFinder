namespace SunshineGameFinder.Core.Utilities;

public static class PathFormatter
{
    public static string FormatPath(string? path)
    {
        if (string.IsNullOrEmpty(path)) 
            return string.Empty;

        path = path.Trim('"');

        if (!path.StartsWith("steam://", StringComparison.OrdinalIgnoreCase))
        {
            path = path.Replace("/", "\\");
        }

        return path;
    }

    public static string FormatEnvPath(string? path)
    {
        if (string.IsNullOrEmpty(path)) 
            return string.Empty;

        var paths = path.Split(';', StringSplitOptions.RemoveEmptyEntries);
        
        var formattedPaths = paths.Select(p => 
        {
            var trimmed = p.Trim().Trim('"');
            return trimmed.Replace("/", "\\").TrimEnd('\\');
        });

        return string.Join(";", formattedPaths);
    }
}

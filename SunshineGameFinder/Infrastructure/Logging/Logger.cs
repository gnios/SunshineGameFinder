namespace SunshineGameFinder.Infrastructure.Logging;

public static class Logger
{
    public static void Log(string message) => Log(message, LogLevel.Information);

    public static void Log(string message, bool newline) => Log(message, LogLevel.Information, newline);

    public static void Log(string message, LogLevel level, bool newline = true)
    {
        var color = level switch
        {
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Success => ConsoleColor.Green,
            LogLevel.Trace => ConsoleColor.Gray,
            _ => ConsoleColor.White
        };

        Console.ForegroundColor = color;

        if (newline)
            Console.WriteLine(message);
        else
            Console.Write(message);

        Console.ForegroundColor = ConsoleColor.White;
    }
}

public enum LogLevel
{
    Error,
    Warning,
    Information,
    Success,
    Trace
}

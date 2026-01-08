using System.Diagnostics;
using SunshineGameFinder.Core.Models;
using SunshineGameFinder.Infrastructure.Logging;

namespace SunshineGameFinder.Services;

internal sealed class StreamingServiceManager
{
    public static async Task RestartServiceAsync(StreamingPlatform platform)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                await RestartWindowsServiceAsync(platform);
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                await RestartUnixServiceAsync(platform);
            }
            else
            {
                Logger.Log("Unsupported OS for restarting service", LogLevel.Warning);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Error restarting service: {ex.Message}", LogLevel.Error);
        }
    }

    private static async Task RestartWindowsServiceAsync(StreamingPlatform platform)
    {
        var serviceName = platform switch
        {
            StreamingPlatform.Sunshine => "SunshineService",
            StreamingPlatform.Apollo => "ApolloService",
            _ => "SunshineService"
        };

        var platformName = platform switch
        {
            StreamingPlatform.Sunshine => "Sunshine",
            StreamingPlatform.Apollo => "Apollo",
            _ => "Sunshine"
        };

        var psi = new ProcessStartInfo("powershell", $"-NoProfile -NonInteractive -Command \"Restart-Service -Name '{serviceName}' -Force\"")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi);
        if (proc == null)
        {
            Logger.Log("Failed to start PowerShell process", LogLevel.Error);
            return;
        }

        await proc.WaitForExitAsync();
        
        var errText = await proc.StandardError.ReadToEndAsync();
        
        if (proc.ExitCode == 0)
        {
            Logger.Log($"{platformName} service reiniciado com sucesso.", LogLevel.Success);
        }
        else
        {
            Logger.Log($"Failed to restart {platformName} service. ExitCode={proc.ExitCode}. {errText}", LogLevel.Warning);
        }
    }

    private static async Task RestartUnixServiceAsync(StreamingPlatform platform)
    {
        var serviceName = platform switch
        {
            StreamingPlatform.Sunshine => "sunshine",
            StreamingPlatform.Apollo => "apollo",
            _ => "sunshine"
        };

        var platformName = platform switch
        {
            StreamingPlatform.Sunshine => "Sunshine",
            StreamingPlatform.Apollo => "Apollo",
            _ => "Sunshine"
        };

        var psi = new ProcessStartInfo("systemctl", $"restart {serviceName}")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi);
        if (proc == null)
        {
            Logger.Log("Failed to start systemctl process", LogLevel.Error);
            return;
        }

        await proc.WaitForExitAsync();
        
        var errText = await proc.StandardError.ReadToEndAsync();
        
        if (proc.ExitCode == 0)
        {
            Logger.Log($"Serviço {platformName.ToLower()} reiniciado com sucesso.", LogLevel.Success);
        }
        else
        {
            Logger.Log($"Failed to restart {platformName.ToLower()} service. ExitCode={proc.ExitCode}. {errText}", LogLevel.Warning);
        }
    }
}

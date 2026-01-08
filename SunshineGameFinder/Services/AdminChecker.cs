using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace SunshineGameFinder.Services;

internal static class AdminChecker
{
    public static bool IsRunAsAdmin()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            
            return Environment.UserName == "root";
        }
        catch
        {
            return false;
        }
    }

    public static void RequestElevation(string[] args)
    {
        var exeName = Process.GetCurrentProcess().MainModule?.FileName;
        if (exeName == null)
            return;

        try
        {
            if (OperatingSystem.IsWindows())
            {
                var processInfo = new ProcessStartInfo(exeName)
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    Arguments = string.Join(" ", args)
                };

                Process.Start(processInfo);
            }
            else
            {
                var processInfo = new ProcessStartInfo("sudo")
                {
                    UseShellExecute = true
                };
                
                processInfo.ArgumentList.Add(exeName);
                foreach (var arg in args)
                {
                    processInfo.ArgumentList.Add(arg);
                }
                
                var process = Process.Start(processInfo);
                process?.WaitForExit();
            }
        }
        catch (Exception ex)
        {
                Infrastructure.Logging.Logger.Log(
                $"This application requires administrative privileges. Elevation failed: {ex.Message}", 
                Infrastructure.Logging.LogLevel.Error);
        }
    }
}

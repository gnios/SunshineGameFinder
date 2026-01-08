using System.CommandLine;
using SunshineGameFinder.Core.Models;

namespace SunshineGameFinder.Services.CommandLine;

internal sealed class CommandLineParser
{
    private Option<string[]>? _addlDirectoriesOption;
    private Option<string[]>? _addlExeExclusionWordsOption;
    private Option<string>? _sunshineConfigLocationOption;
    private Option<string>? _targetPlatformOption;
    private Option<bool>? _forceOption;
    private Option<bool>? _removeUninstalledOption;
    private Option<bool>? _ensureDesktopAppOption;
    private Option<bool>? _ensureSteamBigPictureOption;
    private Option<bool>? _noWaitOption;
    private Option<bool>? _updateImagesOption;

    public (RootCommand RootCommand, ApplicationOptions Options) Parse(string[] args)
    {
        var rootCommand = CreateRootCommand();
        var parseResult = rootCommand.Parse(args);
        var options = ParseOptions(parseResult);
        
        return (rootCommand, options);
    }

    private RootCommand CreateRootCommand()
    {
        _addlDirectoriesOption = new Option<string[]>("--addlDirectories", "-d")
        {
            AllowMultipleArgumentsPerToken = true,
            Description = "Additional platform directories to search. ONLY looks for game directories in the top level of this folder."
        };

        _addlExeExclusionWordsOption = new Option<string[]>("--addlExeExclusionWords", "-exeExclude")
        {
            Description = "Additional words to exclude from exe names when searching for game executables.",
            AllowMultipleArgumentsPerToken = true
        };

        _sunshineConfigLocationOption = new Option<string>("--sunshineConfigLocation", "-c")
        {
            Description = "Specify the apps.json location (Sunshine or Apollo)",
            AllowMultipleArgumentsPerToken = false
        };

        _targetPlatformOption = new Option<string>("--target", "-t")
        {
            Description = "Target platform: 'sunshine' or 'apollo' (default: auto-detect)",
            AllowMultipleArgumentsPerToken = false
        };

        _forceOption = new Option<bool>("--force", "-f")
        {
            Description = "Force re-adding of existing games to Sunshine apps config",
            AllowMultipleArgumentsPerToken = false
        };

        _removeUninstalledOption = new Option<bool>("--remove-uninstalled", "-ru")
        {
            AllowMultipleArgumentsPerToken = false,
            Description = "Removes games from Sunshine apps config that no longer have an executable on disk"
        };

        _ensureDesktopAppOption = new Option<bool>("--ensure-desktop-app", "-desktop")
        {
            AllowMultipleArgumentsPerToken = false,
            Description = "Ensures that the 'Desktop' app is present"
        };

        _ensureSteamBigPictureOption = new Option<bool>("--ensure-steam-big-picture", "-bigpicture")
        {
            AllowMultipleArgumentsPerToken = false,
            Description = "Ensures that the 'Steam Big Picture' app is present"
        };

        _noWaitOption = new Option<bool>("--no-wait")
        {
            AllowMultipleArgumentsPerToken = false
        };

        _updateImagesOption = new Option<bool>("--update-images", "-ui")
        {
            AllowMultipleArgumentsPerToken = false,
            Description = "Update missing images for existing games in the config"
        };

        var rootCommand = new RootCommand(
            "Searches your computer for various common game install paths for Sunshine/Apollo streaming applications. " +
            "After running it, all games that did not already exist will be added to the apps.json, " +
            "meaning your Moonlight client should see them next time it is started.")
        {
            _addlDirectoriesOption,
            _addlExeExclusionWordsOption,
            _sunshineConfigLocationOption,
            _targetPlatformOption,
            _forceOption,
            _removeUninstalledOption,
            _ensureDesktopAppOption,
            _ensureSteamBigPictureOption,
            _noWaitOption,
            _updateImagesOption!
        };

        return rootCommand;
    }

    private ApplicationOptions ParseOptions(ParseResult parseResult)
    {
        var targetPlatformStr = parseResult.GetValue(_targetPlatformOption!);
        StreamingPlatform? targetPlatform = null;
        
        if (!string.IsNullOrEmpty(targetPlatformStr))
        {
            if (Enum.TryParse<StreamingPlatform>(targetPlatformStr, ignoreCase: true, out var platform))
            {
                targetPlatform = platform;
            }
        }

        return new ApplicationOptions
        {
            AdditionalDirectories = parseResult.GetValue(_addlDirectoriesOption!) ?? Array.Empty<string>(),
            AdditionalExeExclusionWords = parseResult.GetValue(_addlExeExclusionWordsOption!) ?? Array.Empty<string>(),
            SunshineConfigLocation = parseResult.GetValue(_sunshineConfigLocationOption!),
            TargetPlatform = targetPlatform,
            ForceUpdate = parseResult.GetValue(_forceOption!),
            RemoveUninstalled = parseResult.GetValue(_removeUninstalledOption!),
            EnsureDesktop = parseResult.GetValue(_ensureDesktopAppOption!),
            EnsureSteamBigPicture = parseResult.GetValue(_ensureSteamBigPictureOption!),
            NoWait = parseResult.GetValue(_noWaitOption!),
            UpdateImages = parseResult.GetValue(_updateImagesOption!)
        };
    }
}

internal sealed class ApplicationOptions
{
    public string[] AdditionalDirectories { get; set; } = Array.Empty<string>();
    public string[] AdditionalExeExclusionWords { get; set; } = Array.Empty<string>();
    public string? SunshineConfigLocation { get; set; }
    public StreamingPlatform? TargetPlatform { get; set; }
    public bool ForceUpdate { get; set; }
    public bool RemoveUninstalled { get; set; }
    public bool EnsureDesktop { get; set; }
    public bool EnsureSteamBigPicture { get; set; }
    public bool NoWait { get; set; }
    public bool UpdateImages { get; set; }
}

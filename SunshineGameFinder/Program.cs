using SunshineGameFinder.Infrastructure.Logging;
using SunshineGameFinder.Services;
using SunshineGameFinder.Services.CommandLine;
using SunshineGameFinder.Services.Configuration;
using SunshineGameFinder.Services.GameDiscovery;

var appVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

if (!AdminChecker.IsRunAsAdmin())
{
    AdminChecker.RequestElevation(args);
    return;
}

Logger.Log($@"
Thanks for using the Sunshine Game Finder! App Version: {appVersion} - Runtime: {Environment.Version}

Searches your computer for various common game install paths for Sunshine/Apollo streaming applications. 
After running it, all games that did not already exist will be added to the apps.json, 
meaning your Moonlight client should see them next time it is started.

Use --target sunshine or --target apollo to specify the platform, or let it auto-detect.

Have an issue or an idea? Come contribute at https://github.com/JMTK/SunshineGameFinder
");

var parser = new CommandLineParser();
var (_, options) = parser.Parse(args);

var configManager = new ConfigManager();
var steamLibraryFinder = new SteamLibraryFinder();
var directoryInitializer = new GameDirectoryInitializer(steamLibraryFinder);
var specialAppEnsurer = new SpecialAppEnsurer();
var uninstalledGameRemover = new UninstalledGameRemover();

var orchestrator = new ApplicationOrchestrator(
    configManager,
    directoryInitializer,
    specialAppEnsurer,
    uninstalledGameRemover);

var exitCode = await orchestrator.RunAsync(options);
Environment.Exit(exitCode);

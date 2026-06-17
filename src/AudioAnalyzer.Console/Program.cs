using System.IO.Abstractions;
using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Console;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Infrastructure;
using AudioAnalyzer.Visualizers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Parse CLI for screen-dump automation (before interactive-console gate so --dump-after can run headless).
int? dumpAfterSeconds = null;
string? dumpPath = null;
string[] cliArgs = Environment.GetCommandLineArgs();
for (int i = 1; i < cliArgs.Length; i++)
{
    if (cliArgs[i] == "--dump-after" && i + 1 < cliArgs.Length && int.TryParse(cliArgs[i + 1], out int dumpN) && dumpN > 0)
    {
        dumpAfterSeconds = dumpN;
        i++;
    }
    else if (cliArgs[i] == "--dump-path" && i + 1 < cliArgs.Length)
    {
        dumpPath = cliArgs[i + 1];
        i++;
    }
}

bool consoleInputReady = InteractiveConsoleInput.IsSupported;

if (!consoleInputReady && dumpAfterSeconds == null)
{
    MacOsLaunchDiagnostics.ReportTerminalRequiredAndExit();
    return;
}

// Load settings before building the renderer so visualizer settings are available for DI
var fileSystem = new FileSystem();
HostContentPaths contentPaths = HostContentPaths.Resolve(fileSystem, PlatformSelection.CreateContentLocator());
var presetRepo = new FilePresetRepository(fileSystem, contentPaths.PresetsDirectory);
var settingsRepo = new FileSettingsRepository(
    fileSystem,
    presetRepo,
    new DefaultTextLayersSettingsFactory(),
    contentPaths.SettingsFilePath);
var settings = settingsRepo.LoadAppSettings();
var visualizerSettings = settingsRepo.LoadVisualizerSettings();

using var provider = ServiceConfiguration.Build(
    settingsRepo,
    presetRepo,
    settings,
    visualizerSettings,
    new ServiceConfigurationOptions
    {
        FileSystem = fileSystem,
        ShowsDirectory = contentPaths.ShowsDirectory,
        ThemesDirectory = contentPaths.ThemesDirectory,
        CharsetsDirectory = contentPaths.CharsetsDirectory,
        WritableDataRoot = contentPaths.WritableDataRoot,
        PaletteRepository = new FilePaletteRepository(fileSystem, contentPaths.PalettesDirectory),
    });
var deviceInfo = provider.GetRequiredService<IAudioDeviceInfo>();

var bootstrapLoggerFactory = provider.GetRequiredService<ILoggerFactory>();
provider.GetRequiredService<IPlatformStartupDiagnostics>().LogStartup();

var devices = deviceInfo.GetDevices();
var (initialDeviceId, initialName) = DeviceResolver.TryResolveFromSettings(
    devices,
    settings,
    provider.GetRequiredService<IDefaultDeviceFallbackPolicy>());
if (initialName == "")
{
    if (dumpAfterSeconds != null)
    {
        var demo = devices.FirstOrDefault(d => d.Id?.StartsWith(DemoAudioDevice.Prefix, StringComparison.Ordinal) == true);
        if (demo != null)
        {
            initialDeviceId = demo.Id;
            initialName = demo.Name;
        }
        else
        {
            (initialDeviceId, initialName) = provider.GetRequiredService<IDeviceSelectionModal>().Show(null, _ => { });
        }
    }
    else if (InteractiveConsoleInput.IsSupported)
    {
        (initialDeviceId, initialName) = provider.GetRequiredService<IDeviceSelectionModal>().Show(null, _ => { });
    }
    else
    {
        var demo = devices.FirstOrDefault(d => d.Id?.StartsWith(DemoAudioDevice.Prefix, StringComparison.Ordinal) == true);
        if (demo != null)
        {
            initialDeviceId = demo.Id;
            initialName = demo.Name;
        }
    }
}

if (initialName == "")
{
    Console.WriteLine("No device selected.");
    return;
}

var shell = provider.GetRequiredService<ApplicationShell>();
var engine = provider.GetRequiredService<AnalysisEngine>();
engine.BeatSensitivity = settings.BeatSensitivity;
provider.GetRequiredService<IWaveformHistoryConfigurator>().ApplyMaxHistorySeconds(settings.MaxAudioHistorySeconds, null);

StartupLogging.LogApplicationStarted(bootstrapLoggerFactory.CreateLogger("AudioAnalyzer"));

shell.Run(initialDeviceId, initialName, dumpAfterSeconds, dumpPath);

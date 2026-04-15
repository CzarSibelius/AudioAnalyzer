using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Console;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Infrastructure;
using AudioAnalyzer.Visualizers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Parse CLI for screen-dump automation
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

// Load settings before building the renderer so visualizer settings are available for DI
var presetRepo = new FilePresetRepository();
var settingsRepo = new FileSettingsRepository(presetRepo, new DefaultTextLayersSettingsFactory());
var settings = settingsRepo.LoadAppSettings();
var visualizerSettings = settingsRepo.LoadVisualizerSettings();

using var provider = ServiceConfiguration.Build(settingsRepo, presetRepo, settings, visualizerSettings);
var deviceInfo = provider.GetRequiredService<IAudioDeviceInfo>();

var devices = deviceInfo.GetDevices();
var (initialDeviceId, initialName) = DeviceResolver.TryResolveFromSettings(devices, settings);
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
    else
    {
        (initialDeviceId, initialName) = provider.GetRequiredService<IDeviceSelectionModal>().Show(null, _ => { });
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

ILoggerFactory loggerFactory = provider.GetRequiredService<ILoggerFactory>();
StartupLogging.LogApplicationStarted(loggerFactory.CreateLogger("AudioAnalyzer"));

shell.Run(initialDeviceId, initialName, dumpAfterSeconds, dumpPath);

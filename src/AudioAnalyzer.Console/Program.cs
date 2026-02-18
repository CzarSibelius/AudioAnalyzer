using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Console;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

// Load settings before building the renderer so visualizer settings are available for DI
var presetRepo = new FilePresetRepository();
var settingsRepo = new FileSettingsRepository(presetRepo);
var settings = settingsRepo.LoadAppSettings();
var visualizerSettings = settingsRepo.LoadVisualizerSettings();

using var provider = ServiceConfiguration.Build(settingsRepo, presetRepo, settings, visualizerSettings);
var deviceInfo = provider.GetRequiredService<IAudioDeviceInfo>();

var devices = deviceInfo.GetDevices();
var (initialDeviceId, initialName) = DeviceResolver.TryResolveFromSettings(devices, settings);
if (initialName == "")
{
    (initialDeviceId, initialName) = DeviceSelectionModal.Show(deviceInfo, settingsRepo, settings, null, _ => { });
}

if (initialName == "")
{
    Console.WriteLine("No device selected.");
    return;
}

var engine = provider.GetRequiredService<AnalysisEngine>();
engine.BeatSensitivity = settings.BeatSensitivity;

var shell = new ApplicationShell(
    deviceInfo,
    settingsRepo,
    provider.GetRequiredService<IVisualizerSettingsRepository>(),
    settings,
    visualizerSettings,
    provider.GetRequiredService<IPresetRepository>(),
    provider.GetRequiredService<IShowRepository>(),
    provider.GetRequiredService<IPaletteRepository>(),
    engine,
    provider.GetRequiredService<IVisualizationRenderer>(),
    provider.GetRequiredService<INowPlayingProvider>());

shell.Run(initialDeviceId, initialName);

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
    (initialDeviceId, initialName) = provider.GetRequiredService<IDeviceSelectionModal>().Show(null, _ => { });
}

if (initialName == "")
{
    Console.WriteLine("No device selected.");
    return;
}

var shell = provider.GetRequiredService<ApplicationShell>();
var engine = provider.GetRequiredService<AnalysisEngine>();
engine.BeatSensitivity = settings.BeatSensitivity;

shell.Run(initialDeviceId, initialName);

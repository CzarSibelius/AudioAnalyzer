using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;

namespace AudioAnalyzer.Console;

/// <summary>Persists application and visualizer settings from engine and visualizer state.</summary>
internal sealed class AppSettingsPersistence : IAppSettingsPersistence
{
    private readonly AnalysisEngine _engine;
    private readonly VisualizerSettings _visualizerSettings;
    private readonly AppSettings _settings;
    private readonly ISettingsRepository _settingsRepo;
    private readonly IVisualizerSettingsRepository _visualizerSettingsRepo;

    public AppSettingsPersistence(
        AnalysisEngine engine,
        VisualizerSettings visualizerSettings,
        AppSettings settings,
        ISettingsRepository settingsRepo,
        IVisualizerSettingsRepository visualizerSettingsRepo)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _visualizerSettings = visualizerSettings ?? throw new ArgumentNullException(nameof(visualizerSettings));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _settingsRepo = settingsRepo ?? throw new ArgumentNullException(nameof(settingsRepo));
        _visualizerSettingsRepo = visualizerSettingsRepo ?? throw new ArgumentNullException(nameof(visualizerSettingsRepo));
    }

    /// <inheritdoc />
    public void Save()
    {
        _settings.BeatSensitivity = _engine.BeatSensitivity;
        _settings.BeatCircles = _visualizerSettings.TextLayers?.Layers?.FirstOrDefault(l => l.LayerType == TextLayerType.BeatCircles)?.Enabled ?? true;
        var oscLayer = _visualizerSettings.TextLayers?.Layers?.FirstOrDefault(l => l.LayerType == TextLayerType.Oscilloscope);
        _settings.OscilloscopeGain = oscLayer?.GetCustom<OscilloscopeSettings>()?.Gain ?? 2.5;
        _settingsRepo.SaveAppSettings(_settings);
        _visualizerSettings.TextLayers ??= new TextLayersVisualizerSettings();
        _visualizerSettingsRepo.SaveVisualizerSettings(_visualizerSettings);
    }
}

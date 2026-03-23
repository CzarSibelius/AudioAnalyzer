using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>
/// Resolves <see cref="IApplicationMode"/> from <see cref="VisualizerSettings.ApplicationMode"/>.
/// </summary>
internal sealed class ApplicationModeFactory : IApplicationModeFactory
{
    private readonly VisualizerSettings _visualizerSettings;
    private readonly PresetEditorApplicationMode _preset;
    private readonly ShowPlayApplicationMode _show;
    private readonly SettingsApplicationMode _settings;

    public ApplicationModeFactory(
        VisualizerSettings visualizerSettings,
        PresetEditorApplicationMode preset,
        ShowPlayApplicationMode show,
        SettingsApplicationMode settings)
    {
        _visualizerSettings = visualizerSettings ?? throw new ArgumentNullException(nameof(visualizerSettings));
        _preset = preset ?? throw new ArgumentNullException(nameof(preset));
        _show = show ?? throw new ArgumentNullException(nameof(show));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <inheritdoc />
    public IApplicationMode GetActiveApplicationMode()
    {
        return _visualizerSettings.ApplicationMode switch
        {
            ApplicationMode.ShowPlay => _show,
            ApplicationMode.Settings => _settings,
            _ => _preset
        };
    }
}

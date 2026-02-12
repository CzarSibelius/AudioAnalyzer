namespace AudioAnalyzer.Visualizers;

/// <summary>Loads and saves per-visualizer settings. Persisted separately from app-level settings.</summary>
public interface IVisualizerSettingsRepository
{
    /// <summary>Loads visualizer settings from persistent storage.</summary>
    VisualizerSettings LoadVisualizerSettings();

    /// <summary>Persists visualizer settings.</summary>
    void SaveVisualizerSettings(VisualizerSettings settings);
}

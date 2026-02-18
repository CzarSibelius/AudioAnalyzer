namespace AudioAnalyzer.Domain;

/// <summary>
/// Top-level application mode: Preset editor (manual cycling) or Show play (auto-cycling).
/// </summary>
public enum ApplicationMode
{
    /// <summary>Edit presets and layers; manually cycle presets with V.</summary>
    PresetEditor,

    /// <summary>Auto-cycle through a Show's presets; S opens Show edit.</summary>
    ShowPlay
}

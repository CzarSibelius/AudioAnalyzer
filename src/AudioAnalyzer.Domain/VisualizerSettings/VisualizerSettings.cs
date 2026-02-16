namespace AudioAnalyzer.Domain;

/// <summary>
/// Container for per-visualizer settings. Each visualizer that needs configuration has its own property here.
/// </summary>
public class VisualizerSettings
{
    /// <summary>Named TextLayers configurations. At least one required. User switches with V key.</summary>
    public List<Preset> Presets { get; set; } = new();

    /// <summary>Id of the active preset. When null, use Presets[0].</summary>
    public string? ActivePresetId { get; set; }

    /// <summary>Live editing buffer — always the active preset's config. Synced from active preset on load/switch; persisted back to active preset on save.</summary>
    public TextLayersVisualizerSettings? TextLayers { get; set; }

    public UnknownPleasuresVisualizerSettings? UnknownPleasures { get; set; }
}

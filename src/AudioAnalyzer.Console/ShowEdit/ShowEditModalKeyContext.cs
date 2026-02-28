using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Mutable context passed to the show edit modal key handler.</summary>
internal sealed class ShowEditModalKeyContext : IKeyHandlerContext
{
    /// <summary>Current show id. Mutated by the handler.</summary>
    public string? CurrentShowId { get; set; }

    /// <summary>Whether the user is renaming the show. Mutated by the handler.</summary>
    public bool Renaming { get; set; }

    /// <summary>Buffer for the new show name. Mutated by the handler.</summary>
    public string RenameBuffer { get; set; } = "";

    /// <summary>Selected entry index. Mutated by the handler.</summary>
    public int SelectedIndex { get; set; }

    /// <summary>Whether the user is editing duration. Mutated by the handler.</summary>
    public bool EditingDuration { get; set; }

    /// <summary>Buffer for the duration value. Mutated by the handler.</summary>
    public string DurationBuffer { get; set; } = "";

    /// <summary>All shows (for new show naming). Read-only.</summary>
    public required IReadOnlyList<ShowInfo> AllShows { get; init; }

    /// <summary>All presets (for add/cycle). Read-only.</summary>
    public required IReadOnlyList<PresetInfo> AllPresets { get; init; }

    /// <summary>Called when visualizer settings should be persisted.</summary>
    public required Action SaveVisualizerSettings { get; init; }

    /// <summary>Show repository for load/save/create.</summary>
    public required IShowRepository ShowRepo { get; init; }

    /// <summary>Preset repository for resolving preset names.</summary>
    public required IPresetRepository PresetRepo { get; init; }

    /// <summary>Visualizer settings (active show id/name). Handler may mutate.</summary>
    public required VisualizerSettings VisualizerSettings { get; init; }
}

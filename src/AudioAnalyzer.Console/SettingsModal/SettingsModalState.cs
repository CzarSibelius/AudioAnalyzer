namespace AudioAnalyzer.Console;

/// <summary>Mutable state for the settings modal during a session.</summary>
internal sealed class SettingsModalState
{
    /// <summary>Index of the selected layer in the sorted list.</summary>
    public int SelectedLayerIndex { get; set; }

    /// <summary>When true, the left panel highlights the Preset row (above layer 1); when false, <see cref="SelectedLayerIndex"/> selects a layer.</summary>
    public bool LeftPanelPresetSelected { get; set; }

    /// <summary>When <see cref="SettingsModalFocus.PickingPalette"/> is active, true means the picker edits preset <c>TextLayers.PaletteId</c> (no inherit row); false means layer palette.</summary>
    public bool PalettePickerForPresetDefault { get; set; }

    /// <summary>True when renaming the preset.</summary>
    public bool Renaming { get; set; }

    /// <summary>Buffer for preset rename input.</summary>
    public string RenameBuffer { get; set; } = "";

    /// <summary>Current focus (layer list, settings list, renaming, or editing).</summary>
    public SettingsModalFocus Focus { get; set; } = SettingsModalFocus.LayerList;

    /// <summary>Index of the selected setting in the settings list.</summary>
    public int SelectedSettingIndex { get; set; }

    /// <summary>Buffer for text-edit setting input.</summary>
    public string EditingBuffer { get; set; } = "";

    /// <summary>Selected row in the palette picker: layer mode — 0 = inherit, 1..N = repo index + 1; preset-default mode — 0..N-1 = repo palette index.</summary>
    public int PalettePickerSelectedIndex { get; set; }

    /// <summary>Palette id when the picker opened; Esc restores it (Enter saves the previewed choice). Used for layer or preset default palette.</summary>
    public string? PalettePickerOriginalPaletteId { get; set; }

    /// <summary>Selected row in the charset picker (0 = legacy snippets row when present, else first charset).</summary>
    public int CharsetPickerSelectedIndex { get; set; }

    /// <summary>Layer <c>CharsetId</c> when the charset picker opened; Esc restores it.</summary>
    public string? CharsetPickerOriginalCharsetId { get; set; }

    /// <summary>When true, row 0 in the charset picker is legacy TextSnippets mode (reserved; currently unused).</summary>
    public bool CharsetPickerIncludeLegacySnippetsRow { get; set; }

    /// <summary>Last navigation key for rate limiting.</summary>
    public ConsoleKey? LastNavKey { get; set; }

    /// <summary>Timestamp of last nav key for rate limiting.</summary>
    public long LastNavTime { get; set; }
}

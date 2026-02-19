namespace AudioAnalyzer.Console;

/// <summary>Mutable state for the settings modal during a session.</summary>
internal sealed class SettingsModalState
{
    /// <summary>Index of the selected layer in the sorted list.</summary>
    public int SelectedLayerIndex { get; set; }

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

    /// <summary>Last navigation key for rate limiting.</summary>
    public ConsoleKey? LastNavKey { get; set; }

    /// <summary>Timestamp of last nav key for rate limiting.</summary>
    public long LastNavTime { get; set; }
}

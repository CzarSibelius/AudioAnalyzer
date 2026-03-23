namespace AudioAnalyzer.Console;

/// <summary>Mutable UI state for the General Settings hub (menu selection and application name edit).</summary>
internal sealed class GeneralSettingsHubState
{
    /// <summary>Selected menu row index (0 = audio input, 1 = application name, 2 = UI theme).</summary>
    public int SelectedIndex { get; set; }

    /// <summary>When true, keys edit <see cref="RenameBuffer"/> instead of navigating the menu.</summary>
    public bool IsEditingAppName { get; set; }

    /// <summary>Buffer while renaming application display name.</summary>
    public string RenameBuffer { get; set; } = "";

    /// <summary>Resets menu and edit state (e.g. when entering General Settings mode).</summary>
    public void ResetInteraction()
    {
        SelectedIndex = 0;
        IsEditingAppName = false;
        RenameBuffer = "";
    }
}

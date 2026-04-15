namespace AudioAnalyzer.Console;

/// <summary>Mutable UI state for the General Settings hub (menu selection and inline text edits).</summary>
internal sealed class GeneralSettingsHubState
{
    /// <summary>Selected menu row index; see <see cref="GeneralSettingsHubMenuRows"/>.</summary>
    public int SelectedIndex { get; set; }

    /// <summary>When not <see cref="GeneralSettingsHubEditMode.None"/>, keys edit <see cref="RenameBuffer"/>.</summary>
    public GeneralSettingsHubEditMode EditMode { get; set; }

    /// <summary>Buffer while renaming application display name or default asset folder path.</summary>
    public string RenameBuffer { get; set; } = "";

    /// <summary>Resets menu and edit state (e.g. when entering General Settings mode).</summary>
    public void ResetInteraction()
    {
        SelectedIndex = 0;
        EditMode = GeneralSettingsHubEditMode.None;
        RenameBuffer = "";
    }
}

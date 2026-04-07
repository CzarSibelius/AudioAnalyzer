namespace AudioAnalyzer.Console;

/// <summary>Sub-flow inside the UI theme selection modal.</summary>
internal enum UiThemeAuthoringPhase
{
    /// <summary>Choose (Custom) or an existing theme file.</summary>
    PickTheme,

    /// <summary>Choose a layer palette as color source for a new theme.</summary>
    NewPickPalette,

    /// <summary>Assign palette indices to UI/title-bar slots.</summary>
    NewEditSlots
}

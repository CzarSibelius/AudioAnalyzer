namespace AudioAnalyzer.Console;

/// <summary>Focus state for the TextLayers settings modal (S key).</summary>
internal enum SettingsModalFocus
{
    LayerList,
    SettingsList,
    Renaming,
    EditingSetting,
}

/// <summary>Edit mode for a setting in the settings modal.</summary>
internal enum SettingEditMode
{
    Cycle,
    TextEdit,
}

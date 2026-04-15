namespace AudioAnalyzer.Console;

/// <summary>Focus state for the TextLayers settings modal (S key).</summary>
internal enum SettingsModalFocus
{
    LayerList,
    SettingsList,
    Renaming,
    EditingSetting,

    /// <summary>Right column shows palette list (inherit + repo palettes).</summary>
    PickingPalette,

    /// <summary>Right column shows charset list (ADR-0080).</summary>
    PickingCharset,
}

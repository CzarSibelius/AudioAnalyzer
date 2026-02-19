namespace AudioAnalyzer.Console;

/// <summary>TextLayers settings overlay modal: layer list, settings panel, preset rename and create.</summary>
internal interface ISettingsModal
{
    /// <summary>Shows the settings overlay modal. Blocks until user closes with ESC.</summary>
    /// <param name="consoleLock">Lock object for console access during modal.</param>
    /// <param name="saveSettings">Callback invoked when settings should be persisted.</param>
    void Show(object consoleLock, Action saveSettings);
}

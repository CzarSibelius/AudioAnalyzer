namespace AudioAnalyzer.Console;

/// <summary>TextLayers settings overlay modal: layer list, settings panel, preset rename and create.</summary>
internal interface ISettingsModal
{
    /// <summary>Shows the settings overlay modal. Blocks until user closes with ESC.</summary>
    /// <param name="consoleLock">Lock object for console access during modal.</param>
    /// <param name="saveSettings">Callback invoked when settings should be persisted.</param>
    /// <param name="setShellModalOpen">When set, passed to nested overlays (e.g. layer type picker on L) so their open/close toggles the shell render guard; S itself does not toggle the guard so the visualizer keeps animating under the overlay.</param>
    void Show(object consoleLock, Action saveSettings, Action<bool>? setShellModalOpen = null);
}

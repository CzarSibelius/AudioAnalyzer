namespace AudioAnalyzer.Console;

/// <summary>Show edit overlay modal: edit Show entries (add/remove/reorder presets, set duration).</summary>
internal interface IShowEditModal
{
    /// <summary>Shows the Show edit overlay modal. Blocks until user closes with ESC.</summary>
    /// <param name="consoleLock">Lock object for console access during modal.</param>
    /// <param name="saveVisualizerSettings">Callback invoked when visualizer settings should be persisted.</param>
    void Show(object consoleLock, Action saveVisualizerSettings);
}

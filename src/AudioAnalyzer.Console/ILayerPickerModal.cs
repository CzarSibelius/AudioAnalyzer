using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Overlay modal to pick a <see cref="TextLayerType"/> for the active digit slot from Preset editor.</summary>
internal interface ILayerPickerModal
{
    /// <summary>
    /// Shows every <see cref="TextLayerType"/> for the slot selected by 1–9 (Z-order sorted). On Enter, applies the
    /// highlighted type to that slot via <paramref name="renderer"/> and invokes <paramref name="persistAndRedraw"/>.
    /// On Esc, closes without changes.
    /// </summary>
    /// <param name="restoreTitleBarViewWhenClosed">
    /// When opening from a parent overlay (e.g. S), set together with <paramref name="restoreOverlayRowCountWhenClosed"/>
    /// so closing the picker restores title bar and overlay rows instead of returning to the main canvas.
    /// </param>
    /// <param name="restoreOverlayRowCountWhenClosed">Parent overlay row count to restore; null means clear overlay (main-canvas picker).</param>
    void Show(
        object consoleLock,
        Action<bool> setModalOpen,
        IVisualizationRenderer renderer,
        VisualizerSettings visualizerSettings,
        IVisualizer visualizer,
        Action persistAndRedraw,
        TitleBarViewKind? restoreTitleBarViewWhenClosed = null,
        int? restoreOverlayRowCountWhenClosed = null);
}

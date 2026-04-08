using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Console;

/// <summary>
/// Orchestrates display and rendering: holds display state (fullscreen, overlay), header callbacks,
/// render guard and console lock, and drives periodic header refresh and visualizer render using
/// analysis results from the analysis engine.
/// </summary>
/// <remarks>
/// <strong>Responsibility boundary.</strong> Implementations own the render pipeline (when and how to run one frame).
/// Full-screen and other display state are owned by <see cref="IDisplayState"/> and injected where needed.
/// The application shell configures the orchestrator (callbacks, guard, lock), drives display cadence via
/// <see cref="Redraw"/> / <see cref="RedrawWithFullHeader"/>, and feeds analysis via <see cref="OnAudioData"/> (no full render there).
/// </remarks>
internal interface IVisualizationOrchestrator
{
    /// <summary>
    /// Configures header callbacks. Main content start row follows the active application mode header line count.
    /// </summary>
    /// <param name="redrawHeader">Full redraw (clear + header), e.g. on resize or keypress.</param>
    /// <param name="refreshHeader">Redraw only the header lines (no clear), called before each render.</param>
    void SetHeaderCallback(Action? redrawHeader, Action? refreshHeader);

    /// <summary>
    /// When set, the orchestrator skips rendering when the guard returns false (e.g. when a modal is open).
    /// </summary>
    void SetRenderGuard(Func<bool>? guard);

    /// <summary>
    /// Optional lock for console output. When set, the orchestrator acquires it before header refresh and render.
    /// </summary>
    void SetConsoleLock(object? consoleLock);

    /// <summary>
    /// When overlay is active, the visualizer starts below the overlay rows. Call with active=false to restore.
    /// </summary>
    void SetOverlayActive(bool active, int overlayRowCount = 0);

    /// <summary>
    /// Refreshes only the header (device, now-playing, BPM, volume). Called by a periodic UI timer (ADR-0038).
    /// </summary>
    void RefreshHeaderIfNeeded();

    /// <summary>Redraw the toolbar and visualizer once using current dimensions and last snapshot data.</summary>
    void Redraw();

    /// <summary>Full redraw: clears console, draws header, then toolbar and visualizer.</summary>
    void RedrawWithFullHeader();

    /// <summary>
    /// Feeds audio to the analysis engine only. Full main-area render is driven by the shell main loop calling
    /// <see cref="Redraw"/> / <see cref="RedrawWithFullHeader"/>. Call from the audio capture callback.
    /// </summary>
    void OnAudioData(byte[] buffer, int bytesRecorded, AudioFormat format);

    /// <summary>
    /// Current frame context for UI (e.g. settings modal palette phase and layer timings). Merges <see cref="AudioAnalysisSnapshot"/>
    /// from the engine with cached <see cref="VisualizationFrameContext.LayerRenderTimeMs"/> when <c>ShowLayerRenderTime</c> is enabled (ADR-0073).
    /// Layout fields may be default when not produced by a full main render this tick.
    /// </summary>
    VisualizationFrameContext GetFrameForUi();
}

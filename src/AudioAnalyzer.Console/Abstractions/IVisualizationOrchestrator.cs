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
/// The application shell configures the orchestrator (callbacks, guard, lock) and triggers Redraw/RedrawWithFullHeader;
/// the orchestrator executes one frame (header + snapshot + render) and, when receiving audio, drives throttled render.
/// </remarks>
internal interface IVisualizationOrchestrator
{
    /// <summary>
    /// Configures header callbacks and the row where the header starts.
    /// </summary>
    /// <param name="redrawHeader">Full redraw (clear + header), e.g. on resize or keypress.</param>
    /// <param name="refreshHeader">Redraw only the header lines (no clear), called before each render.</param>
    /// <param name="startRow">Row index where the header starts.</param>
    void SetHeaderCallback(Action? redrawHeader, Action? refreshHeader, int startRow);

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
    /// Feeds audio to the analysis engine and, when the update interval has elapsed, refreshes header and renders.
    /// Call from the audio capture callback.
    /// </summary>
    void OnAudioData(byte[] buffer, int bytesRecorded, AudioFormat format);
}

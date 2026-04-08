using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Per main-render frame context: <see cref="Analysis"/> from the engine plus terminal layout and optional render instrumentation.
/// Assembled by the visualization orchestrator; consumed by <see cref="IVisualizationRenderer"/> and <see cref="IVisualizer"/>.
/// </summary>
public sealed class VisualizationFrameContext
{
    /// <summary>Audio analysis for this frame (cloned arrays from the engine).</summary>
    public required AudioAnalysisSnapshot Analysis { get; set; }

    public int DisplayStartRow { get; set; }
    public int TerminalWidth { get; set; }
    public int TerminalHeight { get; set; }

    /// <summary>
    /// When show-render-FPS is enabled in UI settings, smoothed full main render FPS (ADR-0067); otherwise unset.
    /// </summary>
    public double? MeasuredMainRenderFps { get; set; }

    /// <summary>
    /// When layer render timing is enabled, per sorted-layer-index <c>Draw</c> duration in milliseconds from the last main render (ADR-0073).
    /// Length is <see cref="TextLayersLimits.MaxLayerCount"/>; <c>null</c> entries mean not timed. Not produced by <see cref="AnalysisEngine"/>; merged in <see cref="IVisualizationOrchestrator.GetFrameForUi"/> for modals.
    /// </summary>
    public double?[]? LayerRenderTimeMs { get; set; }

    /// <summary>
    /// Wall-clock seconds since the previous full main render tick, set by the visualization orchestrator (ADR-0072).
    /// Not set by <see cref="AnalysisEngine"/>; used for delta-time animation in the visualizer and UI scrolling.
    /// </summary>
    public double FrameDeltaSeconds { get; set; }
}
